import type { ApiProblem } from '../../features/vehicles/dtos/vehicleDtos';
export class ApiError extends Error {
  constructor(public status: number, public problem: ApiProblem) {
    super(problem.detail ?? problem.title ?? 'The request failed.');
  }
}
function parseProblem(value: unknown): ApiProblem {
  if (!value || typeof value !== 'object') return { detail: 'The request failed.' };
  const source = value as Record<string, unknown>;
  const problem: ApiProblem = {};
  if (typeof source.title === 'string') problem.title = source.title;
  if (typeof source.detail === 'string') problem.detail = source.detail;
  if (source.errors && typeof source.errors === 'object' && !Array.isArray(source.errors)) {
    problem.errors = Object.create(null) as Record<string, string[]>;
    for (const [field, messages] of Object.entries(source.errors)) {
      if (Array.isArray(messages)) {
        const validMessages = messages.filter((message): message is string => typeof message === 'string');
        if (validMessages.length) problem.errors[field] = validMessages;
      }
    }
  }
  return problem;
}
export async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const timeout = AbortSignal.timeout(15000);
  const signal = options?.signal ? AbortSignal.any([options.signal, timeout]) : timeout;
  let response: Response;
  try {
    response = await fetch(`/api${path}`, { ...options, signal, redirect: 'error', headers: { 'Content-Type': 'application/json', ...options?.headers } });
  } catch (error) {
    if (options?.signal?.aborted) throw error;
    throw new ApiError(0, { detail: timeout.aborted ? 'The request timed out. Please try again.' : 'Cannot reach the API. Check that it is running over HTTPS and try again.' });
  }
  if (!response.ok) {
    const problem = parseProblem(await response.json().catch(() => null));
    throw new ApiError(response.status, problem);
  }
  if (response.status === 204) return undefined as T;
  try {
    return await response.json() as T;
  } catch {
    throw new ApiError(502, { detail: 'The API returned an invalid response. Please try again.' });
  }
}
