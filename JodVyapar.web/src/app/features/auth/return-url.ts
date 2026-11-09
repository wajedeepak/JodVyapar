/** Only follow in-app return URLs (no external redirects, no bouncing back to auth pages). */
export function safeReturnUrl(returnUrl: string | undefined): string {
  if (!returnUrl || !returnUrl.startsWith('/') || returnUrl.startsWith('//') || returnUrl.startsWith('/auth')) {
    return '/';
  }
  return returnUrl;
}
