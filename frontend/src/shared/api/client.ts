import type { ApiProblem } from '../../features/vehicles/dtos/vehicleDtos';
export class ApiError extends Error {
  constructor(public status: number, public problem: ApiProblem) {
    super(problem.detail ?? problem.title ?? 'The request failed.');
  }
}
export async function request<T>(path: string, options?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`/api${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...options?.headers } });
  } catch {
    throw new ApiError(0, { detail: 'Cannot reach the API. Check that Docker is running and try again.' });
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ detail: 'The request failed.' })) as ApiProblem;
    throw new ApiError(response.status, problem);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}
