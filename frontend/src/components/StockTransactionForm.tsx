import { useEffect, useRef, useState, type FormEvent } from 'react'
import type { Item } from '../types/item'
import type { Store } from '../types/store'
import type { TransactionType, StockTransactionPayload } from '../types/transaction'
import { getItems } from '../api/items'
import { getStores } from '../api/stores'
import { createReceiveTransaction, createIssueTransaction } from '../api/transactions'
import { getAvailableStock } from '../api/stockBalance'
import { extractError } from '../api/errors'

interface DetailRow {
  itemId: string
  quantity: string
  unit: string
  remarks: string
  available: number | null
}

interface FormState {
  transactionNo: string
  transactionDate: string
  transactionType: TransactionType
  storeId: string
  remarks: string
}

interface FormErrors {
  transactionNo?: string
  transactionDate?: string
  storeId?: string
  rows: string[]
}

function emptyDetail(): DetailRow {
  return { itemId: '', quantity: '', unit: '', remarks: '', available: null }
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

function initialForm(): FormState {
  return {
    transactionNo: '',
    transactionDate: today(),
    transactionType: 'Receive',
    storeId: '',
    remarks: '',
  }
}

export default function StockTransactionForm() {
  const [items, setItems] = useState<Item[]>([])
  const [stores, setStores] = useState<Store[]>([])
  const [metaLoading, setMetaLoading] = useState(true)
  const [form, setForm] = useState<FormState>(initialForm)
  const [details, setDetails] = useState<DetailRow[]>([emptyDetail()])
  const [errors, setErrors] = useState<FormErrors>({ rows: [] })
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    ;(async () => {
      setMetaLoading(true)
      try {
        const [i, s] = await Promise.all([getItems(), getStores()])
        if (!active) return
        setItems(i)
        setStores(s)
      } catch {
        // selects will remain empty; submission will surface the server error
      } finally {
        if (active) setMetaLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [])

  const detailsRef = useRef(details)
  useEffect(() => {
    detailsRef.current = details
  }, [details])

  function fetchAvailable(index: number, storeId: number, itemId: number) {
    setDetails((prev) => prev.map((r, i) => (i === index ? { ...r, available: null } : r)))
    getAvailableStock(storeId, itemId)
      .then((qty) => setDetails((prev) => prev.map((r, i) => (i === index ? { ...r, available: qty } : r))))
      .catch(() => {
        /* leave available as null */
      })
  }

  // Re-fetch available stock for Issue when the store or type changes.
  useEffect(() => {
    if (form.transactionType !== 'Issue' || !form.storeId) return
    const storeId = Number(form.storeId)
    detailsRef.current.forEach((row, i) => {
      if (row.itemId) fetchAvailable(i, storeId, Number(row.itemId))
    })
  }, [form.storeId, form.transactionType])

  function updateForm<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [field]: value }))
    setSuccess(null)
    setSubmitError(null)
  }

  function addRow() {
    setDetails((d) => [...d, emptyDetail()])
  }

  function removeRow(index: number) {
    setDetails((d) => d.filter((_, i) => i !== index))
  }

  function updateRow<K extends keyof DetailRow>(index: number, field: K, value: DetailRow[K]) {
    setDetails((d) => d.map((r, i) => (i === index ? { ...r, [field]: value } : r)))
  }

  function onItemChange(index: number, itemIdStr: string) {
    const item = items.find((i) => String(i.id) === itemIdStr)
    const newUnit = item?.unit ?? ''
    setDetails((d) =>
      d.map((r, i) => (i === index ? { ...r, itemId: itemIdStr, unit: newUnit, available: null } : r)),
    )
    if (form.transactionType === 'Issue' && form.storeId && itemIdStr) {
      fetchAvailable(index, Number(form.storeId), Number(itemIdStr))
    }
  }

  function validate(): boolean {
    const e: FormErrors = { rows: [] }
    if (!form.transactionNo.trim()) e.transactionNo = 'Transaction No. is required'
    if (!form.transactionDate) e.transactionDate = 'Transaction Date is required'
    if (!form.storeId) e.storeId = 'Store is required'
    e.rows = details.map((row) => {
      const re: string[] = []
      if (!row.itemId) re.push('Item is required')
      const qty = Number(row.quantity)
      if (row.quantity.trim() === '' || isNaN(qty)) re.push('Quantity is required')
      else if (qty <= 0) re.push('Quantity must be > 0')
      return re.join('; ')
    })
    setErrors(e)
    const hasRowError = e.rows.some((r) => r !== '')
    return !e.transactionNo && !e.transactionDate && !e.storeId && !hasRowError && details.length > 0
  }

  async function handleSubmit(ev: FormEvent) {
    ev.preventDefault()
    setSubmitError(null)
    setSuccess(null)
    if (!validate()) return
    setSubmitting(true)
    try {
      const payload: StockTransactionPayload = {
        transactionNo: form.transactionNo.trim(),
        transactionDate: form.transactionDate || null,
        storeId: Number(form.storeId),
        remarks: form.remarks.trim() || null,
        details: details.map((d) => ({
          itemId: Number(d.itemId),
          quantity: Number(d.quantity),
          unit: d.unit,
          remarks: d.remarks.trim() || null,
        })),
      }
      if (form.transactionType === 'Receive') {
        await createReceiveTransaction(payload)
      } else {
        await createIssueTransaction(payload)
      }
      setSuccess(`${form.transactionType} transaction '${form.transactionNo}' created.`)
      setForm(initialForm())
      setDetails([emptyDetail()])
      setErrors({ rows: [] })
    } catch (err) {
      setSubmitError(extractError(err))
    } finally {
      setSubmitting(false)
    }
  }

  if (metaLoading) {
    return <p>Loading form...</p>
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="form-section">
        <h3>Header</h3>
        <div className="form-grid">
          <div className="form-field">
            <label>Transaction No.</label>
            <input
              value={form.transactionNo}
              onChange={(e) => updateForm('transactionNo', e.target.value)}
              disabled={submitting}
            />
            {errors.transactionNo && <div className="field-error">{errors.transactionNo}</div>}
          </div>
          <div className="form-field">
            <label>Transaction Date</label>
            <input
              type="date"
              value={form.transactionDate}
              onChange={(e) => updateForm('transactionDate', e.target.value)}
              disabled={submitting}
            />
            {errors.transactionDate && <div className="field-error">{errors.transactionDate}</div>}
          </div>
          <div className="form-field">
            <label>Transaction Type</label>
            <select
              value={form.transactionType}
              onChange={(e) => updateForm('transactionType', e.target.value as TransactionType)}
              disabled={submitting}
            >
              <option value="Receive">Receive</option>
              <option value="Issue">Issue</option>
            </select>
          </div>
          <div className="form-field">
            <label>Store</label>
            <select
              value={form.storeId}
              onChange={(e) => updateForm('storeId', e.target.value)}
              disabled={submitting}
            >
              <option value="">Select store</option>
              {stores.map((s) => (
                <option key={s.id} value={String(s.id)}>
                  {s.code} - {s.name}
                </option>
              ))}
            </select>
            {errors.storeId && <div className="field-error">{errors.storeId}</div>}
          </div>
          <div className="form-field form-field-wide">
            <label>Remarks</label>
            <input
              value={form.remarks}
              onChange={(e) => updateForm('remarks', e.target.value)}
              disabled={submitting}
            />
          </div>
        </div>
      </div>

      <div className="form-section">
        <div className="section-header">
          <h3>Details</h3>
          <button type="button" className="primary" onClick={addRow} disabled={submitting}>
            + Add Row
          </button>
        </div>
        <table className="data-table detail-grid">
          <thead>
            <tr>
              <th>Item</th>
              <th>Quantity</th>
              <th>Unit</th>
              <th>Remarks</th>
              {form.transactionType === 'Issue' && <th>Available</th>}
              <th></th>
            </tr>
          </thead>
          <tbody>
            {details.map((row, index) => {
              const qty = Number(row.quantity)
              const exceeds =
                form.transactionType === 'Issue' &&
                row.available !== null &&
                row.quantity.trim() !== '' &&
                !isNaN(qty) &&
                qty > row.available
              return (
                <tr key={index}>
                  <td>
                    <select
                      value={row.itemId}
                      onChange={(e) => onItemChange(index, e.target.value)}
                      disabled={submitting}
                    >
                      <option value="">Select item</option>
                      {items.map((i) => (
                        <option key={i.id} value={String(i.id)}>
                          {i.itemCode} - {i.itemName}
                        </option>
                      ))}
                    </select>
                    {errors.rows[index] && <div className="field-error">{errors.rows[index]}</div>}
                  </td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      step="any"
                      value={row.quantity}
                      onChange={(e) => updateRow(index, 'quantity', e.target.value)}
                      disabled={submitting}
                    />
                    {exceeds && <div className="field-warning">Exceeds available stock</div>}
                  </td>
                  <td>
                    <input
                      value={row.unit}
                      onChange={(e) => updateRow(index, 'unit', e.target.value)}
                      disabled={submitting}
                    />
                  </td>
                  <td>
                    <input
                      value={row.remarks}
                      onChange={(e) => updateRow(index, 'remarks', e.target.value)}
                      disabled={submitting}
                    />
                  </td>
                  {form.transactionType === 'Issue' && (
                    <td>{row.available !== null ? row.available : '-'}</td>
                  )}
                  <td>
                    <button
                      type="button"
                      onClick={() => removeRow(index)}
                      disabled={submitting || details.length === 1}
                    >
                      Remove
                    </button>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {submitError && <div className="alert alert-error">{submitError}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="form-actions">
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Saving...' : 'Save Transaction'}
        </button>
      </div>
    </form>
  )
}
