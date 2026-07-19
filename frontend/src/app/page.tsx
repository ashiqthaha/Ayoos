import { ApiStatusCard } from "@/components/api-status-card";

const capabilities = [
  "Practice management",
  "Scheduling",
  "Video consultations",
  "FHIR-native records",
];

export default function Home() {
  return (
    <main className="relative min-h-screen overflow-hidden bg-[#f4f8f3] px-6 py-8 text-[#17382d] sm:px-10 lg:px-16">
      <div className="pointer-events-none absolute -right-36 -top-36 h-96 w-96 rounded-full bg-[#a8d5bb]/40 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-48 -left-24 h-[30rem] w-[30rem] rounded-full bg-[#dcebd6] blur-3xl" />

      <div className="relative mx-auto flex min-h-[calc(100vh-4rem)] max-w-7xl flex-col">
        <header className="flex items-center justify-between border-b border-[#17382d]/10 pb-6">
          <div className="flex items-center gap-3">
            <span className="grid h-10 w-10 place-items-center rounded-2xl bg-[#176b4d] text-lg font-semibold text-white shadow-lg shadow-[#176b4d]/15">
              A
            </span>
            <span className="text-xl font-semibold tracking-[-0.03em]">Ayoos</span>
          </div>
          <span className="rounded-full border border-[#176b4d]/15 bg-white/70 px-4 py-2 text-xs font-medium uppercase tracking-[0.16em] text-[#176b4d] backdrop-blur">
            Open telehealth
          </span>
        </header>

        <section className="grid flex-1 items-center gap-14 py-16 lg:grid-cols-[minmax(0,1fr)_26rem] lg:gap-24">
          <div className="max-w-3xl">
            <p className="mb-6 text-sm font-semibold uppercase tracking-[0.22em] text-[#2d8060]">
              Care infrastructure, reimagined
            </p>
            <h1 className="max-w-3xl text-5xl font-semibold leading-[0.98] tracking-[-0.055em] text-[#12372b] sm:text-6xl lg:text-7xl">
              Better tools for more human care.
            </h1>
            <p className="mt-8 max-w-2xl text-lg leading-8 text-[#47665b] sm:text-xl">
              Ayoos is an open, self-hostable telehealth EMR bringing daily
              operations and clinical records into one calm, connected workspace.
            </p>

            <ul className="mt-10 grid gap-3 text-sm text-[#35594c] sm:grid-cols-2">
              {capabilities.map((capability) => (
                <li
                  key={capability}
                  className="flex items-center gap-3 rounded-2xl border border-white/80 bg-white/55 px-4 py-3 backdrop-blur"
                >
                  <span className="h-2 w-2 rounded-full bg-[#46a878]" />
                  {capability}
                </li>
              ))}
            </ul>
          </div>

          <ApiStatusCard />
        </section>

        <footer className="flex flex-col gap-2 border-t border-[#17382d]/10 py-6 text-sm text-[#658076] sm:flex-row sm:items-center sm:justify-between">
          <span>Built for practices that want control of their care platform.</span>
          <span>.NET Clean Architecture + Next.js</span>
        </footer>
      </div>
    </main>
  );
}
