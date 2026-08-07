import { Component, type ReactNode } from 'react'

type Props = {
  children: ReactNode
  fallbackTitle?: string
}

type State = {
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  render() {
    if (this.state.error) {
      return (
        <div className="rounded-2xl border border-[var(--sf-danger)]/30 bg-[#fdf2f2] px-4 py-3 text-sm">
          <p className="font-semibold text-[var(--sf-danger)]">
            {this.props.fallbackTitle ?? 'Something went wrong'}
          </p>
          <p className="mt-1 text-[var(--sf-muted)]">{this.state.error.message}</p>
          <button
            type="button"
            className="mt-3 rounded-lg border border-black/10 bg-white px-3 py-1.5"
            onClick={() => this.setState({ error: null })}
          >
            Try again
          </button>
        </div>
      )
    }

    return this.props.children
  }
}
