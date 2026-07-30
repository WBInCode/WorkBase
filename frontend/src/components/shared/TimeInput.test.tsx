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

  // Wczesniej dwukropek byl wycinany, a cyfry czytane sztywno jako HH+MM:
  // „9:00" stawalo sie „900" -> godzina 90 -> wpis odrzucany bez slowa.
  describe('rozpoznawanie wpisanej godziny', () => {
    const przypadki: [string, string][] = [
      ['9:00', '09:00'],
      ['9:5', '09:50'],
      ['09:00', '09:00'],
      ['17:30', '17:30'],
      ['9', '09:00'],
      ['17', '17:00'],
      ['930', '09:30'],
      ['1730', '17:30'],
      ['0800', '08:00'],
    ];

    it.each(przypadki)('„%s" daje %s', (wpisane, oczekiwane) => {
      const onChange = vi.fn();
      render(<TimeInput value="" onChange={onChange} />);
      fireEvent.change(screen.getByPlaceholderText('HH:mm'), { target: { value: wpisane } });
      expect(onChange).toHaveBeenLastCalledWith(oczekiwane);
    });

    const bezsensowne = ['99', '2560', '45:00', ':'];

    it.each(bezsensowne)('„%s" nie jest przyjmowane', (wpisane) => {
      const onChange = vi.fn();
      render(<TimeInput value="" onChange={onChange} />);
      fireEvent.change(screen.getByPlaceholderText('HH:mm'), { target: { value: wpisane } });
      expect(onChange).not.toHaveBeenCalled();
    });
  });
});