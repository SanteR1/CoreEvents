import { useTheme, type ThemePreference } from '@/shared/lib/theme';

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const options: { value: ThemePreference; label: string; icon: string }[] = [
    { value: 'light', label: 'Светлая', icon: '☀️' },
    { value: 'system', label: 'Система', icon: '💻' },
    { value: 'dark', label: 'Тёмная', icon: '🌙' },
  ];

  return (
    <div className="inline-flex rounded-lg border border-(--border) bg-(--code-bg) p-1">
      {options.map((option) => {
        const isActive = theme === option.value;
        return (
          <button
            key={option.value}
            type="button"
            onClick={() => setTheme(option.value)}
            className={`flex items-center gap-1.5 rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
              isActive
                ? 'bg-(--bg) text-(--text-h) shadow-sm'
                : 'text-(--text) hover:text-(--text-h)'
            }`}
            title={option.label}
          >
            <span>{option.icon}</span>
            <span>{option.label}</span>
          </button>
        );
      })}
    </div>
  );
}
