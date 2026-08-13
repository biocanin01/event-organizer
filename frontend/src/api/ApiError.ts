export class ApiError extends Error {
  public readonly status: number
  public readonly errors: readonly string[]
  public readonly conflicts: readonly unknown[]

  constructor(
    status: number,
    message: string,
    errors: readonly string[] = [],
    conflicts: readonly unknown[] = [],
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
    this.conflicts = conflicts
  }
}
