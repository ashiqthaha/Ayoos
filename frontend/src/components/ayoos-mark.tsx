import Link from "next/link";

export function AyoosMark() {
  return (
    <Link href="/" className="inline-flex items-center gap-3 text-slate-900">
      <span className="grid h-10 w-10 place-items-center rounded-xl bg-teal-700 text-base font-semibold text-white shadow-md shadow-teal-800/15">
        A
      </span>
      <span className="text-lg font-semibold tracking-[-0.03em]">Ayoos</span>
    </Link>
  );
}
