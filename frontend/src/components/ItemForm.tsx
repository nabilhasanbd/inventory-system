import { useState, type FormEvent } from 'react'
import type { Item, CreateItemPayload, UpdateItemPayload } from '../types/item'
import { extractError } from '../api/errors'

interface ItemFormProps {
  mode: 'create' | 'edit'
  initialItem?: Item
  onSubmit: (payload: CreateItemPayload | UpdateItemPayload) => Promise<void>
  onCancel: () => void
}

interface FieldValues {
  itemCode: string
  itemName: string
  category: string
  unit: string
  reorderLevel: string
  isActive: boolean
}

interface FieldErrors {
  itemCode?: string
  itemName?: string
  category?: string
  unit?: string
  reorderLevel?: string
}

function initialValues(item?: Item | null): FieldValues {
  if (!item) {
    return { itemCode: '', itemName: '', category: '', unit: '', reorderLevel: '', isActive: true }
  }
  return {
    itemCode: item.itemCode,
    itemName: item.itemName,
    category: item.category ?? '',
    unit: item.unit,
    reorderLevel: String(item.reorderLevel),
    isActive: item.isActive,
  }
}

export default function ItemForm({ mode, initialItem, onSubmit, onCancel }: ItemFormProps) {
  const [values, setValues] = useState<FieldValues>(() => initialValues(initialItem))
  const [errors, setErrors] = useState<FieldErrors>({})
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  function validate(): boolean {
    const e: FieldErrors = {}
    if (!values.itemCode.trim()) e.itemCode = 'Item code is required'
    if (!values.itemName.trim()) e.itemName = 'Item name is required'
    if (!values.category.trim()) e.category = 'Category is required'
    if (!values.unit.trim()) e.unit = 'Unit is required'
    if (values.reorderLevel.trim() === '' || isNaN(Number(values.reorderLevel))) {
      e.reorderLevel = 'Reorder level is required'
    } else if (Number(values.reorderLevel) <= 0) {
      e.reorderLevel = 'Reorder level must be greater than 0'
    }
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
        itemCode: values.itemCode.trim(),
        itemName: values.itemName.trim(),
        category: values.category.trim(),
        unit: values.unit.trim(),
        reorderLevel: Number(values.reorderLevel),
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
        <label>Item Code</label>
        <input
          value={values.itemCode}
          onChange={(e) => setValues((v) => ({ ...v, itemCode: e.target.value }))}
          disabled={submitting}
        />
        {errors.itemCode && <div className="field-error">{errors.itemCode}</div>}
      </div>

      <div className="form-field">
        <label>Item Name</label>
        <input
          value={values.itemName}
          onChange={(e) => setValues((v) => ({ ...v, itemName: e.target.value }))}
          disabled={submitting}
        />
        {errors.itemName && <div className="field-error">{errors.itemName}</div>}
      </div>

      <div className="form-field">
        <label>Category</label>
        <input
          value={values.category}
          onChange={(e) => setValues((v) => ({ ...v, category: e.target.value }))}
          disabled={submitting}
        />
        {errors.category && <div className="field-error">{errors.category}</div>}
      </div>

      <div className="form-field">
        <label>Unit</label>
        <input
          value={values.unit}
          onChange={(e) => setValues((v) => ({ ...v, unit: e.target.value }))}
          disabled={submitting}
        />
        {errors.unit && <div className="field-error">{errors.unit}</div>}
      </div>

      <div className="form-field">
        <label>Reorder Level</label>
        <input
          type="number"
          min="0"
          step="any"
          value={values.reorderLevel}
          onChange={(e) => setValues((v) => ({ ...v, reorderLevel: e.target.value }))}
          disabled={submitting}
        />
        {errors.reorderLevel && <div className="field-error">{errors.reorderLevel}</div>}
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
