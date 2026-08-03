export class ApiError extends Error {
  public readonly status: number
  public readonly errors: readonly string[]

  constructor(
    status: number,
    message: string,
    errors: readonly string[] = [],
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}
