import { ApiError } from '../api/client';
export function ErrorNotice({ error }: { error: Error | null }) {
  if (!error) return null;
  return <div className="notice error" role="alert"><strong>{error instanceof ApiError ? error.problem.title ?? 'Request failed' : 'Request failed'}</strong><p>{error.message}</p>{error instanceof ApiError && error.problem.traceId && <small>Trace ID: {error.problem.traceId}</small>}</div>;
}
