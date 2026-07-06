import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, cleanup } from '@testing-library/react';
import { ErrorBoundary } from './ErrorBoundary';

function Bomb({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) throw new Error('boom');
  return <div>ok content</div>;
}

describe('ErrorBoundary', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('renders children when there is no error', () => {
    render(
      <ErrorBoundary>
        <div>hello world</div>
      </ErrorBoundary>,
    );

    expect(screen.getByText('hello world')).toBeInTheDocument();
  });

  it('renders the default fallback UI when a child throws', () => {
    // React logs the error to console.error; suppress noise in test output.
    vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <ErrorBoundary>
        <Bomb shouldThrow />
      </ErrorBoundary>,
    );

    expect(screen.getByText('Coś poszło nie tak')).toBeInTheDocument();
    expect(screen.queryByText('ok content')).not.toBeInTheDocument();
  });

  it('renders a custom fallback when provided', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <ErrorBoundary fallback={(error) => <div>Custom: {error.message}</div>}>
        <Bomb shouldThrow />
      </ErrorBoundary>,
    );

    expect(screen.getByText('Custom: boom')).toBeInTheDocument();
  });

  it('resets and re-renders children after clicking retry', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    function Wrapper() {
      return (
        <ErrorBoundary>
          <Bomb shouldThrow={false} />
        </ErrorBoundary>
      );
    }

    const { rerender } = render(
      <ErrorBoundary>
        <Bomb shouldThrow />
      </ErrorBoundary>,
    );

    expect(screen.getByText('Coś poszło nie tak')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Spróbuj ponownie'));
    rerender(<Wrapper />);

    expect(screen.getByText('ok content')).toBeInTheDocument();
  });
});
