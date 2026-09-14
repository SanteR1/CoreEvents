import axe, { type RunOptions, type Result } from 'axe-core';
import { vi } from 'vitest';

/**
 * Форматирует нарушения доступности в информативное диагностическое сообщение для логов CI/терминала.
 */
export function formatA11yViolations(violations: Result[]): string {
  if (violations.length === 0) return '';

  const formatted = violations
    .map((v) => {
      const nodesDetails = v.nodes
        .map((node, i) => {
          const target = node.target.join(' ');
          const summary = node.failureSummary ?? 'Нет дополнительного описания';
          return `  Node ${i + 1}:
    Target: ${target}
    HTML:   ${node.html}
    Fix:    ${summary.replace(/\n/g, '\n            ')}`;
        })
        .join('\n\n');

      const impact = (v.impact ?? 'unknown').toUpperCase();

      return `[${impact}] ${v.id}: ${v.help}
Description: ${v.description}
Help URL:    ${v.helpUrl}
Affected Nodes (${v.nodes.length}):
${nodesDetails}`;
    })
    .join('\n\n' + '='.repeat(60) + '\n\n');

  return `\n${'='.repeat(60)}\nAccessibility Violations Found (${violations.length}):\n\n${formatted}\n${'='.repeat(60)}\n`;
}

/**
 * Проверяет DOM-контейнер на соответствие стандартам доступности (WCAG 2.1 A/AA).
 * Изолированно мокает canvas на время проверки с обязательным возвратом прототипа в finally.
 */
export async function checkA11y(container: HTMLElement, options?: RunOptions): Promise<Result[]> {
  // Локальный мок canvas для axe-core без загрязнения глобального прототипа для других тестов
  // eslint-disable-next-line @typescript-eslint/unbound-method
  const originalGetContext = HTMLCanvasElement.prototype.getContext;
  HTMLCanvasElement.prototype.getContext = vi.fn(() => null);

  try {
    const results = await axe.run(container, {
      runOnly: {
        type: 'tag',
        values: ['wcag2a', 'wcag2aa'],
      },
      rules: {
        // Исключаем color-contrast в JSDOM: он не имеет layout engine и даёт ложные результаты.
        // Проверка контраста вынесена в контур реального браузера (Playwright).
        'color-contrast': { enabled: false },
        ...options?.rules,
      },
      ...options,
    });

    if (results.violations.length > 0) {
      throw new Error(formatA11yViolations(results.violations));
    }

    return results.violations;
  } finally {
    // Гарантированно возвращаем оригинальный метод
    HTMLCanvasElement.prototype.getContext = originalGetContext;
  }
}
