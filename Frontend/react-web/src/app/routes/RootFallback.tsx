export function RootFallback() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-(--bg) text-(--text)">
      <div className="flex flex-col items-center gap-3">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-(--border) border-t-(--accent)" />
        <span className="text-sm font-medium text-(--text-h)">Загрузка...</span>
      </div>
    </div>
  );
}
