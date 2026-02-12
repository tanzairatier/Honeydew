/**
 * Standard error shape returned by the API (4xx/5xx).
 * Only API services should depend on this; components use the thrown ApiError.
 */
export interface ApiErrorBody {
  error: string
  code?: string
}

/**
 * Error thrown by apiJson/apiFetch when the API returns an error response.
 * message is the user-facing error text; code can be used for branching (e.g. NotFound, Forbid).
 */
export class ApiError extends Error {
  readonly code: string | undefined
  readonly status: number

  constructor(message: string, status: number, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    Object.setPrototypeOf(this, ApiError.prototype)
  }

  static fromResponse(body: unknown, status: number): ApiError {
    const b = body as ApiErrorBody | null
    const message = b?.error && typeof b.error === 'string' ? b.error : `Request failed: ${status}`
    return new ApiError(message, status, b?.code)
  }
}

/** Use in catch blocks to get a safe, user-facing message from any thrown value. */
export function getErrorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return 'Something went wrong.'
}
