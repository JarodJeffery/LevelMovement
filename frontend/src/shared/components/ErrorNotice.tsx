import { ApiError } from '../api/client';
export function ErrorNotice({ error }: { error: Error | null }) {
  if (!error) return null;
  return <div className="notice error" role="alert"><strong>{error instanceof ApiError ? error.problem.title ?? 'Request failed' : 'Request failed'}</strong>{(!(error instanceof ApiError) || error.message !== error.problem.title) && <p>{error.message}</p>}</div>;
}
