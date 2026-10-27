import { FormEvent, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createInvitation,
  getMe,
  isOwner,
  listMembers,
  updateMember,
} from '../../lib/api'
import { SettingsNav } from './SettingsNav'

const ROLES = ['Owner', 'SecurityManager', 'Analyst', 'Operator', 'Viewer'] as const
const STATUSES = ['Active', 'Invited', 'Disabled'] as const
const INVITE_ROLES = ['SecurityManager', 'Analyst', 'Operator', 'Viewer'] as const

export function UsersSettingsPage() {
  const queryClient = useQueryClient()
  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })
  const membersQuery = useQuery({ queryKey: ['members'], queryFn: listMembers })
  const owner = isOwner(meQuery.data?.organizationRole)

  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState<string>('Analyst')
  const [inviteError, setInviteError] = useState<string | null>(null)
  const [inviteLink, setInviteLink] = useState<string | null>(null)

  const inviteMutation = useMutation({
    mutationFn: () => createInvitation({ email: inviteEmail, role: inviteRole }),
    onSuccess: (result) => {
      const origin = window.location.origin
      setInviteLink(`${origin}/accept-invitation?token=${encodeURIComponent(result.token)}`)
      setInviteEmail('')
      setInviteError(null)
      void queryClient.invalidateQueries({ queryKey: ['members'] })
    },
    onError: (err: Error) => {
      setInviteLink(null)
      setInviteError(err.message)
    },
  })

  const memberMutation = useMutation({
    mutationFn: ({
      memberId,
      role,
      status,
    }: {
      memberId: string
      role?: string
      status?: string
    }) => updateMember(memberId, { role, status }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['members'] })
    },
  })

  function onInvite(event: FormEvent) {
    event.preventDefault()
    inviteMutation.mutate()
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Settings</h1>
        <p className="mt-1 text-[var(--sf-muted)]">Organization profile and membership.</p>
      </div>

      <SettingsNav />

      {membersQuery.isLoading && <p className="text-[var(--sf-muted)]">Loading members…</p>}
      {membersQuery.isError && (
        <p className="text-[var(--sf-danger)]">
          {membersQuery.error instanceof Error
            ? membersQuery.error.message
            : 'Failed to load members'}
        </p>
      )}

      {membersQuery.data && (
        <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/5 text-xs tracking-wide text-[var(--sf-muted)] uppercase">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Email</th>
                <th className="px-4 py-3 font-medium">Role</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {membersQuery.data.map((member) => (
                <tr key={member.id} className="border-t border-black/5">
                  <td className="px-4 py-3 font-medium">
                    {member.firstName} {member.lastName}
                  </td>
                  <td className="px-4 py-3">{member.email}</td>
                  <td className="px-4 py-3">
                    {owner ? (
                      <select
                        value={member.role}
                        disabled={memberMutation.isPending}
                        onChange={(e) =>
                          memberMutation.mutate({ memberId: member.id, role: e.target.value })
                        }
                        className="rounded-lg border border-black/10 bg-white px-2 py-1 text-sm"
                      >
                        {ROLES.map((role) => (
                          <option key={role} value={role}>
                            {role}
                          </option>
                        ))}
                      </select>
                    ) : (
                      member.role
                    )}
                  </td>
                  <td className="px-4 py-3">
                    {owner ? (
                      <select
                        value={member.status}
                        disabled={memberMutation.isPending}
                        onChange={(e) =>
                          memberMutation.mutate({ memberId: member.id, status: e.target.value })
                        }
                        className="rounded-lg border border-black/10 bg-white px-2 py-1 text-sm"
                      >
                        {STATUSES.map((status) => (
                          <option key={status} value={status}>
                            {status}
                          </option>
                        ))}
                      </select>
                    ) : (
                      member.status
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {memberMutation.isError && (
        <p className="text-sm text-[var(--sf-danger)]">
          {memberMutation.error instanceof Error
            ? memberMutation.error.message
            : 'Could not update member'}
        </p>
      )}

      {owner && (
        <form
          onSubmit={(e) => void onInvite(e)}
          className="mx-auto max-w-xl space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6"
        >
          <h2 className="text-lg font-semibold">Invite user</h2>
          <p className="text-sm text-[var(--sf-muted)]">
            Creates an invite link you can share. No email is sent.
          </p>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Email</span>
            <input
              type="email"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              required
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Role</span>
            <select
              value={inviteRole}
              onChange={(e) => setInviteRole(e.target.value)}
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
            >
              {INVITE_ROLES.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
          </label>
          {inviteError && <p className="text-sm text-[var(--sf-danger)]">{inviteError}</p>}
          {inviteLink && (
            <div className="rounded-xl border border-[var(--sf-accent)]/20 bg-[var(--sf-accent-soft)] p-4 text-sm">
              <p className="font-medium">Invite link (copy and share)</p>
              <code className="mt-1 block break-all">{inviteLink}</code>
            </div>
          )}
          <button
            type="submit"
            disabled={inviteMutation.isPending}
            className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white disabled:opacity-60"
          >
            {inviteMutation.isPending ? 'Creating…' : 'Create invitation'}
          </button>
        </form>
      )}

      {!owner && meQuery.isSuccess && (
        <p className="text-sm text-[var(--sf-muted)]">
          Only organization owners can change roles or invite users.
        </p>
      )}
    </div>
  )
}
