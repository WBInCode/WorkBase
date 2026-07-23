import { render, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SsoBridgePage } from './SsoBridgePage';

const signinRedirect = vi.fn();

vi.mock('react-oidc-context', () => ({
  useAuth: () => ({
    isLoading: false,
    isAuthenticated: true,
    signinRedirect,
  }),
}));

describe('SsoBridgePage', () => {
  beforeEach(() => {
    signinRedirect.mockReset();
    window.history.replaceState({}, '', '/auth/sso-bridge?email=employee%40example.test');
  });

  it('forces fresh authentication for the HUB-selected employee', async () => {
    render(<SsoBridgePage />);

    await waitFor(() => expect(signinRedirect).toHaveBeenCalledOnce());
    expect(signinRedirect).toHaveBeenCalledWith({
      login_hint: 'employee@example.test',
      prompt: 'login',
      redirect_uri: `${window.location.origin}/auth/callback`,
    });
  });
});