"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

import {
  addAvailabilityException,
  createAvailability,
  deactivateAvailability,
  getProviderAvailability,
  getProviderSlots,
  removeAvailabilityException,
  updateAvailability,
  type AvailabilityException,
  type AvailabilitySchedule,
  type AvailabilityScheduleInput,
  type AvailabilitySlot,
  type DayOfWeek,
} from "@/lib/api";
import {
  addDaysIso,
  apiTime,
  displayTime,
  shortTime,
  todayIsoDate,
} from "@/lib/providers";

type Props = {
  slug: string;
  providerId: string;
  isActive: boolean;
};

type ScheduleRow = {
  dayOfWeek: DayOfWeek;
  label: string;
  enabled: boolean;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
};

const days: Array<{ value: DayOfWeek; label: string }> = [
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" },
  { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
  { value: 0, label: "Sunday" },
];

function scheduleFromSchedules(schedules: AvailabilitySchedule[]): ScheduleRow[] {
  return days.map((day) => {
    const schedule = schedules.find((item) => item.dayOfWeek === day.value);
    return {
      dayOfWeek: day.value,
      label: day.label,
      enabled: Boolean(schedule),
      startTime: schedule ? shortTime(schedule.startTime) : "09:00",
      endTime: schedule ? shortTime(schedule.endTime) : "17:00",
      slotDurationMinutes: schedule?.slotDurationMinutes ?? 30,
    };
  });
}

function formatDate(value: string, options?: Intl.DateTimeFormatOptions) {
  return new Intl.DateTimeFormat(undefined, options ?? {
    weekday: "short",
    month: "short",
    day: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

export function ProviderAvailabilityEditor({
  slug,
  providerId,
  isActive,
}: Props) {
  const [schedule, setSchedule] = useState<ScheduleRow[]>(() =>
    scheduleFromSchedules([]),
  );
  const [savedSchedules, setSavedSchedules] = useState<AvailabilitySchedule[]>([]);
  const [exceptions, setExceptions] = useState<AvailabilityException[]>([]);
  const [slots, setSlots] = useState<AvailabilitySlot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingRules, setIsSavingRules] = useState(false);
  const [isAddingException, setIsAddingException] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [message, setMessage] = useState<{
    tone: "success" | "error";
    text: string;
  } | null>(null);
  const [exceptionDate, setExceptionDate] = useState(todayIsoDate);
  const [exceptionKind, setExceptionKind] = useState<"unavailable" | "custom">(
    "unavailable",
  );
  const [overrideStart, setOverrideStart] = useState("09:00");
  const [overrideEnd, setOverrideEnd] = useState("17:00");
  const [reason, setReason] = useState("");
  const [fromDate, setFromDate] = useState(todayIsoDate);
  const [toDate, setToDate] = useState(() => addDaysIso(6));
  const [slotError, setSlotError] = useState<string | null>(null);

  async function loadSlots(from = fromDate, to = toDate) {
    setIsLoadingSlots(true);
    setSlotError(null);

    try {
      setSlots(await getProviderSlots(slug, providerId, from, to));
    } catch (error) {
      setSlotError(
        error instanceof Error ? error.message : "Couldn’t generate slot preview.",
      );
    } finally {
      setIsLoadingSlots(false);
    }
  }

  useEffect(() => {
    const controller = new AbortController();

    async function load() {
      setIsLoading(true);
      setMessage(null);

      try {
        const [availability, loadedSlots] = await Promise.all([
          getProviderAvailability(slug, providerId, controller.signal),
          getProviderSlots(
            slug,
            providerId,
            todayIsoDate(),
            addDaysIso(6),
            controller.signal,
          ),
        ]);

        setSavedSchedules(availability.schedules);
        setSchedule(scheduleFromSchedules(availability.schedules));
        setExceptions(availability.exceptions);
        setSlots(loadedSlots);
      } catch (error) {
        if (!controller.signal.aborted) {
          setMessage({
            tone: "error",
            text:
              error instanceof Error
                ? error.message
                : "Couldn’t load provider availability.",
          });
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [providerId, slug]);

  const slotsByDate = useMemo(() => {
    const groups = new Map<string, AvailabilitySlot[]>();
    for (const slot of slots) {
      groups.set(slot.date, [...(groups.get(slot.date) ?? []), slot]);
    }
    return [...groups.entries()];
  }, [slots]);

  function updateSchedule(
    dayOfWeek: DayOfWeek,
    patch: Partial<ScheduleRow>,
  ) {
    setSchedule((current) =>
      current.map((row) =>
        row.dayOfWeek === dayOfWeek ? { ...row, ...patch } : row,
      ),
    );
    setMessage(null);
  }

  async function saveRules() {
    const invalidRow = schedule.find(
      (row) => row.enabled && row.endTime <= row.startTime,
    );
    if (invalidRow) {
      setMessage({
        tone: "error",
        text: `${invalidRow.label} end time must be after its start time.`,
      });
      return;
    }

    setIsSavingRules(true);
    setMessage(null);

    try {
      for (const row of schedule) {
        const existing = savedSchedules.find(
          (item) => item.dayOfWeek === row.dayOfWeek,
        );

        if (!row.enabled && existing) {
          await deactivateAvailability(slug, providerId, existing.id);
          continue;
        }

        if (!row.enabled) continue;

        const input: AvailabilityScheduleInput = {
          dayOfWeek: row.dayOfWeek,
          startTime: apiTime(row.startTime),
          endTime: apiTime(row.endTime),
          slotDurationMinutes: row.slotDurationMinutes,
        };

        if (existing) {
          await updateAvailability(slug, providerId, existing.id, input);
        } else {
          await createAvailability(slug, providerId, input);
        }
      }

      const availability = await getProviderAvailability(slug, providerId);
      setSavedSchedules(availability.schedules);
      setSchedule(scheduleFromSchedules(availability.schedules));
      setExceptions(availability.exceptions);
      setMessage({ tone: "success", text: "Weekly availability saved." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text:
          error instanceof Error
            ? error.message
            : "Couldn’t save weekly availability.",
      });
    } finally {
      setIsSavingRules(false);
    }
  }

  async function addException(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (exceptionKind === "custom" && overrideEnd <= overrideStart) {
      setMessage({
        tone: "error",
        text: "Custom hours must end after they start.",
      });
      return;
    }

    setIsAddingException(true);
    setMessage(null);

    try {
      const added = await addAvailabilityException(slug, providerId, {
        date: exceptionDate,
        isUnavailable: exceptionKind === "unavailable",
        overrideStartTime:
          exceptionKind === "custom" ? apiTime(overrideStart) : null,
        overrideEndTime: exceptionKind === "custom" ? apiTime(overrideEnd) : null,
        reason: reason.trim() || null,
      });
      setExceptions((current) =>
        [...current, added].sort((a, b) => a.date.localeCompare(b.date)),
      );
      setReason("");
      setMessage({ tone: "success", text: "Schedule exception added." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text:
          error instanceof Error
            ? error.message
            : "Couldn’t add the schedule exception.",
      });
    } finally {
      setIsAddingException(false);
    }
  }

  async function deleteException(exceptionId: string) {
    setDeletingId(exceptionId);
    setMessage(null);

    try {
      await removeAvailabilityException(slug, providerId, exceptionId);
      setExceptions((current) =>
        current.filter((item) => item.id !== exceptionId),
      );
      setMessage({ tone: "success", text: "Schedule exception removed." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text:
          error instanceof Error
            ? error.message
            : "Couldn’t remove the schedule exception.",
      });
    } finally {
      setDeletingId(null);
    }
  }

  if (isLoading) {
    return (
      <div className="grid gap-5" role="status">
        <div className="h-80 animate-pulse rounded-3xl bg-white" />
        <div className="h-64 animate-pulse rounded-3xl bg-white" />
        <span className="sr-only">Loading availability…</span>
      </div>
    );
  }

  return (
    <div className="grid gap-6">
      {!isActive && (
        <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm leading-6 text-amber-800">
          This provider is inactive, so the slot preview remains empty. Saved
          schedule settings are retained.
        </div>
      )}

      {message && (
        <div
          role="status"
          className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
            message.tone === "success"
              ? "border-emerald-200 bg-emerald-50 text-emerald-800"
              : "border-rose-200 bg-rose-50 text-rose-800"
          }`}
        >
          {message.text}
        </div>
      )}

      <section className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
        <div className="flex flex-col gap-4 border-b border-slate-100 px-6 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-8">
          <div>
            <h2 className="text-xl font-semibold tracking-[-0.025em] text-slate-950">
              Weekly schedule
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              Enable each day and set bookable hours.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void saveRules()}
            disabled={isSavingRules}
            className="w-fit rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:opacity-60"
          >
            {isSavingRules ? "Saving…" : "Save weekly hours"}
          </button>
        </div>

        <div className="divide-y divide-slate-100">
          {schedule.map((row) => (
            <div
              key={row.dayOfWeek}
              className={`grid gap-4 px-6 py-5 sm:grid-cols-[9rem_1fr] sm:items-center sm:px-8 lg:grid-cols-[9rem_1fr_1fr_10rem] ${
                row.enabled ? "" : "bg-slate-50/50"
              }`}
            >
              <label className="flex items-center gap-3 text-sm font-semibold text-slate-800">
                <input
                  type="checkbox"
                  checked={row.enabled}
                  onChange={(event) =>
                    updateSchedule(row.dayOfWeek, {
                      enabled: event.target.checked,
                    })
                  }
                  className="h-5 w-5 rounded border-slate-300 accent-teal-700"
                />
                {row.label}
              </label>
              <label className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                Start
                <input
                  type="time"
                  value={row.startTime}
                  disabled={!row.enabled}
                  onChange={(event) =>
                    updateSchedule(row.dayOfWeek, {
                      startTime: event.target.value,
                    })
                  }
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium normal-case tracking-normal text-slate-800 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 disabled:bg-slate-100 disabled:text-slate-400"
                />
              </label>
              <label className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                End
                <input
                  type="time"
                  value={row.endTime}
                  disabled={!row.enabled}
                  onChange={(event) =>
                    updateSchedule(row.dayOfWeek, {
                      endTime: event.target.value,
                    })
                  }
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium normal-case tracking-normal text-slate-800 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 disabled:bg-slate-100 disabled:text-slate-400"
                />
              </label>
              <label className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                Slot length
                <select
                  value={row.slotDurationMinutes}
                  disabled={!row.enabled}
                  onChange={(event) =>
                    updateSchedule(row.dayOfWeek, {
                      slotDurationMinutes: Number(event.target.value),
                    })
                  }
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium normal-case tracking-normal text-slate-800 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 disabled:bg-slate-100 disabled:text-slate-400"
                >
                  {[15, 20, 30, 45, 60].map((minutes) => (
                    <option key={minutes} value={minutes}>
                      {minutes} min
                    </option>
                  ))}
                </select>
              </label>
            </div>
          ))}
        </div>
      </section>

      <section className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
        <div className="border-b border-slate-100 px-6 py-6 sm:px-8">
          <h2 className="text-xl font-semibold tracking-[-0.025em] text-slate-950">
            Exceptions
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Add time off or custom hours for a single date.
          </p>
        </div>

        <form
          onSubmit={addException}
          className="grid gap-4 bg-teal-50/40 px-6 py-6 sm:px-8 lg:grid-cols-[1fr_1.2fr_1fr]"
        >
          <label className="text-sm font-medium text-slate-700">
            Date
            <input
              type="date"
              required
              value={exceptionDate}
              onChange={(event) => setExceptionDate(event.target.value)}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
            />
          </label>
          <label className="text-sm font-medium text-slate-700">
            Change
            <select
              value={exceptionKind}
              onChange={(event) =>
                setExceptionKind(event.target.value as "unavailable" | "custom")
              }
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
            >
              <option value="unavailable">Unavailable all day</option>
              <option value="custom">Custom hours</option>
            </select>
          </label>
          <label className="text-sm font-medium text-slate-700">
            Reason <span className="font-normal text-slate-400">(optional)</span>
            <input
              type="text"
              maxLength={500}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Vacation, training…"
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none placeholder:text-slate-400 focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
            />
          </label>

          {exceptionKind === "custom" && (
            <div className="grid grid-cols-2 gap-3 lg:col-span-2">
              <label className="text-sm font-medium text-slate-700">
                Start
                <input
                  type="time"
                  value={overrideStart}
                  onChange={(event) => setOverrideStart(event.target.value)}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
                />
              </label>
              <label className="text-sm font-medium text-slate-700">
                End
                <input
                  type="time"
                  value={overrideEnd}
                  onChange={(event) => setOverrideEnd(event.target.value)}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
                />
              </label>
            </div>
          )}

          <div className="flex items-end lg:col-start-3">
            <button
              type="submit"
              disabled={isAddingException}
              className="w-full rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:opacity-60"
            >
              {isAddingException ? "Adding…" : "Add exception"}
            </button>
          </div>
        </form>

        <div className="divide-y divide-slate-100">
          {exceptions.length === 0 ? (
            <p className="px-6 py-8 text-center text-sm text-slate-500">
              No schedule exceptions yet.
            </p>
          ) : (
            exceptions.map((exception) => (
              <div
                key={exception.id}
                className="flex flex-col gap-4 px-6 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-8"
              >
                <div>
                  <p className="font-semibold text-slate-900">
                    {formatDate(exception.date, {
                      weekday: "long",
                      month: "long",
                      day: "numeric",
                      year: "numeric",
                    })}
                  </p>
                  <p className="mt-1 text-sm text-slate-500">
                    {exception.isUnavailable
                      ? "Unavailable all day"
                      : `${displayTime(exception.overrideStartTime!)}–${displayTime(exception.overrideEndTime!)}`}
                    {exception.reason ? ` · ${exception.reason}` : ""}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => void deleteException(exception.id)}
                  disabled={deletingId === exception.id}
                  className="w-fit rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-600 transition hover:border-rose-200 hover:bg-rose-50 hover:text-rose-700 disabled:opacity-50"
                >
                  {deletingId === exception.id ? "Removing…" : "Delete"}
                </button>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
        <div className="border-b border-slate-100 px-6 py-6 sm:px-8">
          <h2 className="text-xl font-semibold tracking-[-0.025em] text-slate-950">
            Open slot preview
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Preview the bookable times generated from this schedule.
          </p>
        </div>

        <form
          onSubmit={(event) => {
            event.preventDefault();
            void loadSlots();
          }}
          className="flex flex-col gap-4 border-b border-slate-100 bg-slate-50/60 px-6 py-5 sm:flex-row sm:items-end sm:px-8"
        >
          <label className="flex-1 text-sm font-medium text-slate-700">
            From
            <input
              type="date"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
            />
          </label>
          <label className="flex-1 text-sm font-medium text-slate-700">
            To
            <input
              type="date"
              min={fromDate}
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
            />
          </label>
          <button
            type="submit"
            disabled={isLoadingSlots}
            className="rounded-xl border border-teal-200 bg-white px-5 py-3 text-sm font-semibold text-teal-800 transition hover:bg-teal-50 disabled:opacity-50"
          >
            {isLoadingSlots ? "Generating…" : "Refresh preview"}
          </button>
        </form>

        <div className="px-6 py-6 sm:px-8">
          {slotError ? (
            <p className="rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {slotError}
            </p>
          ) : slotsByDate.length === 0 ? (
            <div className="py-7 text-center">
              <p className="font-semibold text-slate-800">No open slots</p>
              <p className="mt-1 text-sm text-slate-500">
                Enable weekly hours or adjust exceptions for this date range.
              </p>
            </div>
          ) : (
            <div className="grid gap-6">
              {slotsByDate.map(([date, daySlots]) => (
                <div key={date}>
                  <p className="text-sm font-semibold text-slate-800">
                    {formatDate(date)}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {daySlots.map((slot) => (
                      <span
                        key={`${slot.date}-${slot.startTime}`}
                        className="rounded-xl border border-teal-100 bg-teal-50/70 px-3 py-2 text-sm font-semibold text-teal-800"
                      >
                        {displayTime(slot.startTime)}
                      </span>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
