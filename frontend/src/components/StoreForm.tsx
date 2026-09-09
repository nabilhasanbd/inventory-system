import { useState, type FormEvent } from 'react'
import type { Store, CreateStorePayload, UpdateStorePayload } from '../types/store'
import { extractError } from '../api/errors'

interface StoreFormProps {
  mode: 'create' | 'edit'
  initialStore?: Store
  onSubmit: (payload: CreateStorePayload | UpdateStorePayload) => Promise<void>
  onCancel: () => void
}

interface FieldValues {
  code: string
  name: string
  isActive: boolean
}

interface FieldErrors {
  code?: string
  name?: string
}

function initialValues(store?: Store | null): FieldValues {
  if (!store) return { code: '', name: '', isActive: true }
  return { code: store.code, name: store.name, isActive: store.isActive }
}

export default function StoreForm({ mode, initialStore, onSubmit, onCancel }: StoreFormProps) {
  const [values, setValues] = useState<FieldValues>(() => initialValues(initialStore))
  const [errors, setErrors] = useState<FieldErrors>({})
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  function validate(): boolean {
    const e: FieldErrors = {}
    if (!values.code.trim()) e.code = 'Code is required'
    if (!values.name.trim()) e.name = 'Name is required'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  async function handleSubmit(ev: FormEvent) {
    ev.preventDefault()
    setSubmitError(null)
    if (!validate()) return
    setSubmitting(true)
    try {
      const base = {
        code: values.code.trim(),
        name: values.name.trim(),
      }
      if (mode === 'create') {
        await onSubmit({ ...base, isActive: values.isActive })
      } else {
        await onSubmit(base)
      }
    } catch (err) {
      setSubmitError(extractError(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="form-field">
        <label>Code</label>
        <input
          value={values.code}
          onChange={(e) => setValues((v) => ({ ...v, code: e.target.value }))}
          disabled={submitting}
        />
        {errors.code && <div className="field-error">{errors.code}</div>}
      </div>

      <div className="form-field">
        <label>Name</label>
        <input
          value={values.name}
          onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
          disabled={submitting}
        />
        {errors.name && <div className="field-error">{errors.name}</div>}
      </div>

      {mode === 'create' && (
        <div className="form-field">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={values.isActive}
              onChange={(e) => setValues((v) => ({ ...v, isActive: e.target.checked }))}
              disabled={submitting}
            />
            Active
          </label>
        </div>
      )}

      {submitError && <div className="form-error">{submitError}</div>}

      <div className="form-actions">
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Saving...' : 'Save'}
        </button>
      </div>
    </form>
  )
}
