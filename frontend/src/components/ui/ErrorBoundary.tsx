import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface ErrorBoundaryProps {
  children: ReactNode;
  /** Optional custom fallback renderer. Receives the error and a reset callback. */
  fallback?: (error: Error, reset: () => void) => ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

/**
 * Catches rendering errors in the component tree below it and shows a
 * graceful fallback instead of a blank white screen.
 *
 * Note: does not catch errors from async code (e.g. rejected promises in
 * event handlers) or from TanStack Query — those must be handled by the
 * query's own `error` state. This guards against unexpected render-time
 * exceptions (e.g. accessing a property of `undefined`).
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, info.componentStack);
  }

  reset = () => this.setState({ error: null });

  render() {
    const { error } = this.state;
    if (error) {
      if (this.props.fallback) return this.props.fallback(error, this.reset);

      return (
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: '12px',
            padding: '32px',
            margin: '16px',
            borderRadius: '8px',
            backgroundColor: '#fef2f2',
            border: '1px solid #fecaca',
            textAlign: 'center',
          }}
        >
          <AlertTriangle size={32} color="#dc2626" />
          <div style={{ fontSize: '16px', fontWeight: 600, color: '#991b1b' }}>
            Coś poszło nie tak
          </div>
          <div style={{ fontSize: '14px', color: '#7f1d1d', maxWidth: '480px' }}>
            Wystąpił nieoczekiwany błąd podczas wyświetlania tego widoku. Spróbuj odświeżyć stronę.
          </div>
          <button
            onClick={this.reset}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 16px',
              fontSize: '14px',
              fontWeight: 500,
              color: '#fff',
              backgroundColor: '#dc2626',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
            }}
          >
            <RefreshCw size={16} />
            Spróbuj ponownie
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
