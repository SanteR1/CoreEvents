export function RootFallback() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--bg)] text-[var(--text)]">
      <div className="flex flex-col items-center gap-3">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--border)] border-t-[var(--accent)]" />
        <span className="text-sm font-medium text-[var(--text-h)]">Загрузка...</span>
      </div>
    </div>
  );
}
