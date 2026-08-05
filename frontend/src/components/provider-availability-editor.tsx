"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

import {
  createAvailabilityException,
  createAvailabilitySchedule,
  deleteAvailabilityException,
  deleteAvailabilitySchedule,
  getProviderExceptions,
  getProviderSlots,
  getProviderWeeklySchedule,
  updateAvailabilitySchedule,
  type AvailabilityException,
  type AvailabilityOverlapConflict,
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

type WindowDraft = {
  id?: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
};

const days: Array<{ value: DayOfWeek; label: string; shortLabel: string }> = [
  { value: 1, label: "Monday", shortLabel: "Mon" },
  { value: 2, label: "Tuesday", shortLabel: "Tue" },
  { value: 3, label: "Wednesday", shortLabel: "Wed" },
  { value: 4, label: "Thursday", shortLabel: "Thu" },
  { value: 5, label: "Friday", shortLabel: "Fri" },
  { value: 6, label: "Saturday", shortLabel: "Sat" },
  { value: 0, label: "Sunday", shortLabel: "Sun" },
];

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    weekday: "short",
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

function toDraft(schedule: AvailabilitySchedule): WindowDraft {
  return {
    id: schedule.id,
    dayOfWeek: schedule.dayOfWeek,
    startTime: shortTime(schedule.startTime),
    endTime: shortTime(schedule.endTime),
    slotDurationMinutes: schedule.slotDurationMinutes,
  };
}

export function ProviderAvailabilityEditor({
  slug,
  providerId,
  isActive,
}: Props) {
  const [schedules, setSchedules] = useState<AvailabilitySchedule[]>([]);
  const [exceptions, setExceptions] = useState<AvailabilityException[]>([]);
  const [slots, setSlots] = useState<AvailabilitySlot[]>([]);
  const [draft, setDraft] = useState<WindowDraft | null>(null);
  const [conflicts, setConflicts] = useState<AvailabilityOverlapConflict[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingWindow, setIsSavingWindow] = useState(false);
  const [deletingWindowId, setDeletingWindowId] = useState<string | null>(null);
  const [isAddingException, setIsAddingException] = useState(false);
  const [deletingExceptionId, setDeletingExceptionId] = useState<string | null>(null);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [message, setMessage] = useState<{
    tone: "success" | "error";
    text: string;
  } | null>(null);
  const [exceptionDate, setExceptionDate] = useState(todayIsoDate);
  const [exceptionType, setExceptionType] = useState<"unavailable" | "custom">(
    "unavailable",
  );
  const [exceptionStart, setExceptionStart] = useState("09:00");
  const [exceptionEnd, setExceptionEnd] = useState("17:00");
  const [reason, setReason] = useState("");
  const [fromDate, setFromDate] = useState(todayIsoDate);
  const [toDate, setToDate] = useState(() => addDaysIso(6));
  const [slotError, setSlotError] = useState<string | null>(null);

  async function refreshSchedules() {
    const weekly = await getProviderWeeklySchedule(slug, providerId);
    setSchedules(weekly.days.flatMap((day) => day.schedules));
  }

  async function refreshExceptions() {
    setExceptions(
      await getProviderExceptions(
        slug,
        providerId,
        todayIsoDate(),
        addDaysIso(365),
      ),
    );
  }

  async function loadSlots(from = fromDate, to = toDate) {
    if (to < from) {
      setSlotError("The end date must be on or after the start date.");
      return;
    }

    setIsLoadingSlots(true);
    setSlotError(null);
    try {
      setSlots(await getProviderSlots(slug, providerId, from, to));
    } catch (error) {
      setSlotError(
        error instanceof Error ? error.message : "Could not generate slot preview.",
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
        const [weekly, upcoming, loadedSlots] = await Promise.all([
          getProviderWeeklySchedule(slug, providerId, controller.signal),
          getProviderExceptions(
            slug,
            providerId,
            todayIsoDate(),
            addDaysIso(365),
            controller.signal,
          ),
          getProviderSlots(
            slug,
            providerId,
            todayIsoDate(),
            addDaysIso(6),
            controller.signal,
          ),
        ]);
        setSchedules(weekly.days.flatMap((day) => day.schedules));
        setExceptions(upcoming);
        setSlots(loadedSlots);
      } catch (error) {
        if (!controller.signal.aborted) {
          setMessage({
            tone: "error",
            text: error instanceof Error
              ? error.message
              : "Could not load provider availability.",
          });
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [providerId, slug]);

  const schedulesByDay = useMemo(() => {
    const grouped = new Map<DayOfWeek, AvailabilitySchedule[]>();
    for (const day of days) grouped.set(day.value, []);
    for (const schedule of schedules) {
      grouped.get(schedule.dayOfWeek)?.push(schedule);
    }
    for (const daily of grouped.values()) {
      daily.sort((left, right) => left.startTime.localeCompare(right.startTime));
    }
    return grouped;
  }, [schedules]);

  const slotsByDate = useMemo(() => {
    const grouped = new Map<string, AvailabilitySlot[]>();
    for (const slot of slots) {
      grouped.set(slot.date, [...(grouped.get(slot.date) ?? []), slot]);
    }
    return [...grouped.entries()];
  }, [slots]);

  function beginAdd(dayOfWeek: DayOfWeek) {
    setDraft({
      dayOfWeek,
      startTime: "09:00",
      endTime: "17:00",
      slotDurationMinutes: 30,
    });
    setConflicts([]);
    setMessage(null);
  }

  function updateDraft(patch: Partial<WindowDraft>) {
    setDraft((current) => current ? { ...current, ...patch } : current);
    setConflicts([]);
    setMessage(null);
  }

  async function saveWindow(confirmOverlap: boolean) {
    if (!draft) return;
    if (draft.endTime <= draft.startTime) {
      setMessage({ tone: "error", text: "End time must be after start time." });
      return;
    }

    const [startHour, startMinute] = draft.startTime.split(":").map(Number);
    const [endHour, endMinute] = draft.endTime.split(":").map(Number);
    const windowMinutes = endHour * 60 + endMinute - startHour * 60 - startMinute;
    if (
      draft.slotDurationMinutes <= 0 ||
      windowMinutes % draft.slotDurationMinutes !== 0
    ) {
      setMessage({
        tone: "error",
        text: "Slot duration must divide the window evenly.",
      });
      return;
    }

    const input: AvailabilityScheduleInput = {
      dayOfWeek: draft.dayOfWeek,
      startTime: apiTime(draft.startTime),
      endTime: apiTime(draft.endTime),
      slotDurationMinutes: draft.slotDurationMinutes,
      confirmOverlap,
    };

    setIsSavingWindow(true);
    setMessage(null);
    try {
      const result = draft.id
        ? await updateAvailabilitySchedule(slug, providerId, draft.id, input)
        : await createAvailabilitySchedule(slug, providerId, input);

      if (!result.schedule) {
        setConflicts(result.overlapPreview.conflicts);
        return;
      }

      await refreshSchedules();
      setDraft(null);
      setConflicts([]);
      setMessage({ tone: "success", text: "Availability window saved." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text: error instanceof Error ? error.message : "Could not save this window.",
      });
    } finally {
      setIsSavingWindow(false);
    }
  }

  async function removeWindow(scheduleId: string) {
    setDeletingWindowId(scheduleId);
    setMessage(null);
    try {
      await deleteAvailabilitySchedule(slug, providerId, scheduleId);
      if (draft?.id === scheduleId) setDraft(null);
      await refreshSchedules();
      setMessage({ tone: "success", text: "Availability window removed." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text: error instanceof Error ? error.message : "Could not remove this window.",
      });
    } finally {
      setDeletingWindowId(null);
    }
  }

  async function addException(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (exceptionType === "custom" && exceptionEnd <= exceptionStart) {
      setMessage({
        tone: "error",
        text: "Custom-hours end time must be after its start time.",
      });
      return;
    }

    setIsAddingException(true);
    setMessage(null);
    try {
      await createAvailabilityException(slug, providerId, {
        date: exceptionDate,
        exceptionType: exceptionType === "unavailable" ? 0 : 1,
        startTime: exceptionType === "custom" ? apiTime(exceptionStart) : null,
        endTime: exceptionType === "custom" ? apiTime(exceptionEnd) : null,
        reason: reason.trim() || null,
      });
      await refreshExceptions();
      setReason("");
      setMessage({ tone: "success", text: "Availability exception added." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text: error instanceof Error ? error.message : "Could not add the exception.",
      });
    } finally {
      setIsAddingException(false);
    }
  }

  async function removeException(exceptionId: string) {
    setDeletingExceptionId(exceptionId);
    setMessage(null);
    try {
      await deleteAvailabilityException(slug, providerId, exceptionId);
      setExceptions((current) => current.filter((item) => item.id !== exceptionId));
      setMessage({ tone: "success", text: "Availability exception removed." });
      await loadSlots();
    } catch (error) {
      setMessage({
        tone: "error",
        text: error instanceof Error ? error.message : "Could not remove the exception.",
      });
    } finally {
      setDeletingExceptionId(null);
    }
  }

  if (isLoading) {
    return (
      <div className="h-96 animate-pulse rounded-3xl bg-white" role="status">
        <span className="sr-only">Loading provider availability...</span>
      </div>
    );
  }

  return (
    <div className="grid gap-6">
      {!isActive && (
        <p className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          This provider is inactive. Their schedule is retained, but it does not
          generate bookable slots.
        </p>
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

      <section className="rounded-3xl border border-white bg-white p-5 shadow-[0_18px_50px_rgba(15,118,110,0.08)] sm:p-8">
        <div>
          <p className="text-sm font-semibold text-teal-700">Weekly schedule</p>
          <h2 className="mt-1 text-2xl font-semibold tracking-[-0.03em] text-slate-950">
            Recurring time windows
          </h2>
          <p className="mt-2 text-sm leading-6 text-slate-500">
            Add as many windows as needed for each day. Every window has its own
            slot duration.
          </p>
        </div>

        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {days.map((day) => {
            const dailySchedules = schedulesByDay.get(day.value) ?? [];
            const isAddingHere = draft?.dayOfWeek === day.value && !draft.id;
            return (
              <article
                key={day.value}
                className="rounded-2xl border border-slate-200 bg-slate-50/60 p-4"
              >
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.16em] text-teal-700">
                      {day.shortLabel}
                    </p>
                    <h3 className="mt-0.5 font-semibold text-slate-900">{day.label}</h3>
                  </div>
                  <button
                    type="button"
                    disabled={!isActive || draft !== null}
                    onClick={() => beginAdd(day.value)}
                    className="rounded-xl border border-teal-200 bg-white px-3 py-2 text-xs font-semibold text-teal-700 transition hover:bg-teal-50 disabled:cursor-not-allowed disabled:opacity-45"
                  >
                    + Add window
                  </button>
                </div>

                <div className="mt-4 grid gap-3">
                  {dailySchedules.length === 0 && !isAddingHere && (
                    <p className="rounded-xl border border-dashed border-slate-200 bg-white px-3 py-5 text-center text-sm text-slate-400">
                      No hours set
                    </p>
                  )}

                  {dailySchedules.map((schedule) =>
                    draft?.id === schedule.id ? (
                      <WindowForm
                        key={schedule.id}
                        draft={draft}
                        isSaving={isSavingWindow}
                        onChange={updateDraft}
                        onCancel={() => {
                          setDraft(null);
                          setConflicts([]);
                        }}
                        onSave={() => void saveWindow(false)}
                      />
                    ) : (
                      <div
                        key={schedule.id}
                        className="rounded-xl border border-slate-200 bg-white p-3"
                      >
                        <p className="font-semibold text-slate-900">
                          {displayTime(schedule.startTime)} - {displayTime(schedule.endTime)}
                        </p>
                        <p className="mt-1 text-xs text-slate-500">
                          {schedule.slotDurationMinutes}-minute slots
                        </p>
                        <div className="mt-3 flex gap-2">
                          <button
                            type="button"
                            disabled={!isActive || draft !== null}
                            onClick={() => {
                              setDraft(toDraft(schedule));
                              setConflicts([]);
                            }}
                            className="text-xs font-semibold text-teal-700 disabled:opacity-40"
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            disabled={!isActive || deletingWindowId === schedule.id}
                            onClick={() => void removeWindow(schedule.id)}
                            className="text-xs font-semibold text-rose-600 disabled:opacity-40"
                          >
                            {deletingWindowId === schedule.id ? "Removing..." : "Remove"}
                          </button>
                        </div>
                      </div>
                    ),
                  )}

                  {isAddingHere && draft && (
                    <WindowForm
                      draft={draft}
                      isSaving={isSavingWindow}
                      onChange={updateDraft}
                      onCancel={() => {
                        setDraft(null);
                        setConflicts([]);
                      }}
                      onSave={() => void saveWindow(false)}
                    />
                  )}
                </div>
              </article>
            );
          })}
        </div>

        {conflicts.length > 0 && draft && (
          <div className="mt-6 rounded-2xl border border-amber-200 bg-amber-50 p-5">
            <p className="text-sm font-semibold text-amber-900">
              This window overlaps saved availability
            </p>
            <p className="mt-1 text-sm leading-6 text-amber-800">
              Review the conflicts below. You can keep editing, or confirm this
              overlap intentionally.
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              {conflicts.map((conflict) => (
                <span
                  key={conflict.id}
                  className="rounded-lg border border-amber-200 bg-white px-3 py-2 text-sm font-medium text-amber-900"
                >
                  {displayTime(conflict.startTime)} - {displayTime(conflict.endTime)} ·{" "}
                  {conflict.slotDurationMinutes} min
                </span>
              ))}
            </div>
            <button
              type="button"
              disabled={isSavingWindow}
              onClick={() => void saveWindow(true)}
              className="mt-4 rounded-xl bg-amber-700 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              {isSavingWindow ? "Confirming..." : "Confirm and save overlap"}
            </button>
          </div>
        )}
      </section>

      <section className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
        <div className="border-b border-slate-100 px-5 py-6 sm:px-8">
          <p className="text-sm font-semibold text-teal-700">Exceptions</p>
          <h2 className="mt-1 text-2xl font-semibold tracking-[-0.03em] text-slate-950">
            Date-specific changes
          </h2>
        </div>
        <div className="grid lg:grid-cols-[1.05fr_0.95fr]">
          <form onSubmit={addException} className="grid gap-4 p-5 sm:p-8">
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="text-sm font-semibold text-slate-700">
                Date
                <input
                  type="date"
                  min={todayIsoDate()}
                  value={exceptionDate}
                  onChange={(event) => setExceptionDate(event.target.value)}
                  className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2.5 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
                  required
                />
              </label>
              <label className="text-sm font-semibold text-slate-700">
                Change
                <select
                  value={exceptionType}
                  onChange={(event) => setExceptionType(
                    event.target.value as "unavailable" | "custom",
                  )}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
                >
                  <option value="unavailable">Unavailable all day</option>
                  <option value="custom">Custom hours</option>
                </select>
              </label>
            </div>

            {exceptionType === "custom" && (
              <div className="grid grid-cols-2 gap-4">
                <TimeField label="Start" value={exceptionStart} onChange={setExceptionStart} />
                <TimeField label="End" value={exceptionEnd} onChange={setExceptionEnd} />
              </div>
            )}

            <label className="text-sm font-semibold text-slate-700">
              Reason <span className="font-normal text-slate-400">(optional)</span>
              <input
                value={reason}
                maxLength={500}
                onChange={(event) => setReason(event.target.value)}
                placeholder="Conference, holiday, extended clinic..."
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2.5 outline-none placeholder:text-slate-300 focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
              />
            </label>
            <button
              type="submit"
              disabled={!isActive || isAddingException}
              className="w-fit rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:opacity-50"
            >
              {isAddingException ? "Adding..." : "Add exception"}
            </button>
          </form>

          <div className="border-t border-slate-100 bg-slate-50/60 p-5 sm:p-8 lg:border-l lg:border-t-0">
            <h3 className="font-semibold text-slate-900">Upcoming exceptions</h3>
            <div className="mt-4 grid gap-3">
              {exceptions.length === 0 ? (
                <p className="rounded-xl border border-dashed border-slate-200 bg-white p-5 text-center text-sm text-slate-500">
                  No exceptions in the next 365 days.
                </p>
              ) : (
                exceptions.map((exception) => (
                  <div
                    key={exception.id}
                    className="flex items-start justify-between gap-3 rounded-xl border border-slate-200 bg-white p-4"
                  >
                    <div>
                      <p className="text-sm font-semibold text-slate-900">
                        {formatDate(exception.date)}
                      </p>
                      <p className="mt-1 text-sm text-slate-600">
                        {exception.exceptionType === 0
                          ? "Unavailable all day"
                          : `${displayTime(exception.startTime!)} - ${displayTime(exception.endTime!)}`}
                      </p>
                      {exception.reason && (
                        <p className="mt-1 text-xs text-slate-400">{exception.reason}</p>
                      )}
                    </div>
                    <button
                      type="button"
                      disabled={!isActive || deletingExceptionId === exception.id}
                      onClick={() => void removeException(exception.id)}
                      className="text-xs font-semibold text-rose-600 disabled:opacity-40"
                    >
                      {deletingExceptionId === exception.id ? "Removing..." : "Remove"}
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void loadSlots();
          }}
          className="flex flex-col gap-4 border-b border-slate-100 px-5 py-6 sm:px-8 lg:flex-row lg:items-end lg:justify-between"
        >
          <div>
            <p className="text-sm font-semibold text-teal-700">Slot preview</p>
            <h2 className="mt-1 text-2xl font-semibold tracking-[-0.03em] text-slate-950">
              Materialized bookable slots
            </h2>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
            <label className="text-sm font-semibold text-slate-700">
              From
              <input
                type="date"
                value={fromDate}
                onChange={(event) => setFromDate(event.target.value)}
                className="mt-2 block rounded-xl border border-slate-200 px-3 py-2.5 outline-none focus:border-teal-500"
                required
              />
            </label>
            <label className="text-sm font-semibold text-slate-700">
              To
              <input
                type="date"
                value={toDate}
                onChange={(event) => setToDate(event.target.value)}
                className="mt-2 block rounded-xl border border-slate-200 px-3 py-2.5 outline-none focus:border-teal-500"
                required
              />
            </label>
            <button
              type="submit"
              disabled={isLoadingSlots}
              className="rounded-xl border border-teal-200 px-4 py-2.5 text-sm font-semibold text-teal-700 transition hover:bg-teal-50 disabled:opacity-50"
            >
              {isLoadingSlots ? "Generating..." : "Refresh preview"}
            </button>
          </div>
        </form>

        <div className="p-5 sm:p-8">
          {slotError ? (
            <p className="rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {slotError}
            </p>
          ) : slotsByDate.length === 0 ? (
            <div className="py-8 text-center">
              <p className="font-semibold text-slate-800">No open slots</p>
              <p className="mt-1 text-sm text-slate-500">
                Add weekly hours or adjust exceptions for this date range.
              </p>
            </div>
          ) : (
            <div className="grid gap-6">
              {slotsByDate.map(([date, dailySlots]) => (
                <div key={date}>
                  <p className="text-sm font-semibold text-slate-800">{formatDate(date)}</p>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {dailySlots.map((slot) => (
                      <span
                        key={`${slot.date}-${slot.startTime}-${slot.availabilityScheduleId ?? "custom"}`}
                        className="rounded-xl border border-teal-100 bg-teal-50/70 px-3 py-2 text-sm font-semibold text-teal-800"
                      >
                        {displayTime(slot.startTime)} - {displayTime(slot.endTime)}
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

function WindowForm({
  draft,
  isSaving,
  onChange,
  onCancel,
  onSave,
}: {
  draft: WindowDraft;
  isSaving: boolean;
  onChange: (patch: Partial<WindowDraft>) => void;
  onCancel: () => void;
  onSave: () => void;
}) {
  return (
    <div className="rounded-xl border border-teal-200 bg-white p-3 shadow-sm">
      <div className="grid grid-cols-2 gap-3">
        <TimeField
          label="Start"
          value={draft.startTime}
          onChange={(value) => onChange({ startTime: value })}
        />
        <TimeField
          label="End"
          value={draft.endTime}
          onChange={(value) => onChange({ endTime: value })}
        />
      </div>
      <label className="mt-3 block text-xs font-semibold text-slate-600">
        Slot duration
        <select
          value={draft.slotDurationMinutes}
          onChange={(event) => onChange({ slotDurationMinutes: Number(event.target.value) })}
          className="mt-1.5 w-full rounded-lg border border-slate-200 bg-white px-2.5 py-2 text-sm outline-none focus:border-teal-500"
        >
          {[10, 15, 20, 30, 45, 60, 90, 120].map((duration) => (
            <option key={duration} value={duration}>{duration} minutes</option>
          ))}
        </select>
      </label>
      <div className="mt-3 flex justify-end gap-2">
        <button
          type="button"
          disabled={isSaving}
          onClick={onCancel}
          className="rounded-lg px-3 py-2 text-xs font-semibold text-slate-500"
        >
          Cancel
        </button>
        <button
          type="button"
          disabled={isSaving}
          onClick={onSave}
          className="rounded-lg bg-teal-700 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50"
        >
          {isSaving ? "Saving..." : "Save"}
        </button>
      </div>
    </div>
  );
}

function TimeField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="text-xs font-semibold text-slate-600">
      {label}
      <input
        type="time"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1.5 w-full rounded-lg border border-slate-200 px-2.5 py-2 text-sm outline-none focus:border-teal-500"
        required
      />
    </label>
  );
}
