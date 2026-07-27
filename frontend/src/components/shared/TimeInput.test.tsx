import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import TimeInput from './TimeInput';

const originalScrollIntoView = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'scrollIntoView');

describe('TimeInput', () => {
  afterEach(() => {
    cleanup();
    if (originalScrollIntoView) {
      Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', originalScrollIntoView);
    } else {
      Reflect.deleteProperty(HTMLElement.prototype, 'scrollIntoView');
    }
  });

  it('does not scroll an ancestor when opening the time options', () => {
    let scrollContainer: HTMLElement | null = null;
    const scrollIntoView = vi.fn(() => {
      if (scrollContainer) scrollContainer.scrollTop = 0;
    });
    Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
      configurable: true,
      value: scrollIntoView,
    });

    render(
      <div data-testid="scroll-container">
        <TimeInput value="08:00" onChange={vi.fn()} />
      </div>,
    );
    scrollContainer = screen.getByTestId('scroll-container');
    scrollContainer.scrollTop = 120;

    fireEvent.focus(screen.getByPlaceholderText('HH:mm'));

    expect(screen.getByText('08:00')).toBeInTheDocument();
    expect(scrollContainer.scrollTop).toBe(120);
    expect(scrollIntoView).not.toHaveBeenCalled();
  });
});