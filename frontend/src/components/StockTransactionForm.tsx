import { useEffect, useRef, useState, type FormEvent } from 'react'
import type { Item } from '../types/item'
import type { Store } from '../types/store'
import type { StockTransaction, TransactionType, StockTransactionPayload, UpdateStockTransactionPayload } from '../types/transaction'
import { getItems } from '../api/items'
import { getStores } from '../api/stores'
import { createReceiveTransaction, createIssueTransaction, getStockTransaction, updateStockTransaction } from '../api/transactions'
import { getAvailableStock } from '../api/stockBalance'
import { extractError } from '../api/errors'

interface DetailRow {
  id?: number
  itemId: string
  quantity: string
  unit: string
  remarks: string
  available: number | null
  stockNotice: string | null
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
  details?: string
  rows: string[]
}

function emptyDetail(): DetailRow {
  return { itemId: '', quantity: '', unit: '', remarks: '', available: null, stockNotice: null }
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

interface StockTransactionFormProps {
  transactionId?: number
}

export default function StockTransactionForm({ transactionId }: StockTransactionFormProps) {
  const isEditing = transactionId !== undefined
  const [items, setItems] = useState<Item[]>([])
  const [stores, setStores] = useState<Store[]>([])
  const [metaLoading, setMetaLoading] = useState(true)
  const [metaError, setMetaError] = useState<string | null>(null)
  const [transactionLoading, setTransactionLoading] = useState(isEditing)
  const [transactionError, setTransactionError] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(initialForm)
  const [details, setDetails] = useState<DetailRow[]>([emptyDetail()])
  const [errors, setErrors] = useState<FormErrors>({ rows: [] })
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [originalIssueQuantities, setOriginalIssueQuantities] = useState<Record<string, number>>({})

  useEffect(() => {
    let active = true
    ;(async () => {
      setMetaLoading(true)
      try {
        const [i, s] = await Promise.all([getItems(), getStores()])
        if (!active) return
        setItems(i)
        setStores(s)
      } catch (err) {
        if (active) setMetaError(extractError(err))
      } finally {
        if (active) setMetaLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [])

  function applyTransaction(transaction: StockTransaction) {
    if (transaction.transactionType !== 'Receipt' && transaction.transactionType !== 'Issue') {
      throw new Error(`Transaction type '${transaction.transactionType}' cannot be edited in this form.`)
    }

    setForm({
      transactionNo: transaction.transactionNo,
      transactionDate: transaction.transactionDate.slice(0, 10),
      transactionType: transaction.transactionType === 'Receipt' ? 'Receive' : 'Issue',
      storeId: String(transaction.storeId),
      remarks: transaction.remarks ?? '',
    })
    setDetails(
      transaction.details.map((detail) => ({
        id: detail.id,
        itemId: String(detail.itemId),
        quantity: String(detail.quantity),
        unit: detail.unit,
        remarks: detail.remarks ?? '',
        available: null,
        stockNotice: null,
      })),
    )
    setOriginalIssueQuantities(
      transaction.transactionType === 'Issue'
        ? transaction.details.reduce<Record<string, number>>((totals, detail) => {
            const key = String(detail.itemId)
            totals[key] = (totals[key] ?? 0) + detail.quantity
            return totals
          }, {})
        : {},
    )
    if (transaction.transactionType === 'Issue') {
      transaction.details.forEach((detail, index) => {
        fetchAvailable(index, transaction.storeId, detail.itemId)
      })
    }
  }

  useEffect(() => {
    if (!isEditing || transactionId === undefined) return
    let active = true
    setTransactionLoading(true)
    getStockTransaction(transactionId)
      .then((transaction) => {
        if (active) applyTransaction(transaction)
      })
      .catch((err) => {
        if (active) setTransactionError(extractError(err))
      })
      .finally(() => {
        if (active) setTransactionLoading(false)
      })
    return () => {
      active = false
    }
  }, [isEditing, transactionId])

  const detailsRef = useRef(details)
  const stockRequestIds = useRef<number[]>([])
  useEffect(() => {
    detailsRef.current = details
  }, [details])

  function fetchAvailable(index: number, storeId: number, itemId: number) {
    const requestId = (stockRequestIds.current[index] ?? 0) + 1
    stockRequestIds.current[index] = requestId
    setDetails((prev) => prev.map((r, i) => (i === index ? { ...r, available: null, stockNotice: null } : r)))
    getAvailableStock(storeId, itemId)
      .then((qty) =>
        setDetails((prev) =>
          prev.map((r, i) =>
            i === index && stockRequestIds.current[index] === requestId && r.itemId === String(itemId)
              ? { ...r, available: qty }
              : r,
          ),
        ),
      )
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
    setErrors((e) => ({ ...e, rows: [...e.rows, ''] }))
  }

  function removeRow(index: number) {
    setDetails((d) => d.filter((_, i) => i !== index))
    setErrors((e) => ({ ...e, rows: e.rows.filter((_, i) => i !== index) }))
  }

  function updateRow<K extends keyof DetailRow>(index: number, field: K, value: DetailRow[K]) {
    setDetails((d) => d.map((r, i) => (i === index ? { ...r, [field]: value } : r)))
  }

  function availableForRow(index: number, rows = details): number | null {
    const row = rows[index]
    if (row.available === null || !row.itemId) return null
    const originalIssued = form.transactionType === 'Issue' && isEditing ? (originalIssueQuantities[row.itemId] ?? 0) : 0

    const quantityInOtherRows = rows.reduce((total, other, otherIndex) => {
      if (otherIndex === index || other.itemId !== row.itemId) return total
      const quantity = Number(other.quantity)
      return total + (Number.isFinite(quantity) && quantity > 0 ? quantity : 0)
    }, 0)

    return Math.max(0, row.available + originalIssued - quantityInOtherRows)
  }

  function availableTotalForItem(itemId: string): number | null {
    const row = details.find((detail) => detail.itemId === itemId && detail.available !== null)
    if (!row || row.available === null) return null
    const originalIssued = form.transactionType === 'Issue' && isEditing ? (originalIssueQuantities[itemId] ?? 0) : 0
    return row.available + originalIssued
  }

  function onQuantityChange(index: number, value: string) {
    if (form.transactionType !== 'Issue' || value.trim() === '') {
      updateRow(index, 'quantity', value)
      return
    }

    const quantity = Number(value)
    const limit = availableForRow(index)
    if (Number.isFinite(quantity) && limit !== null && quantity > limit) {
      setDetails((rows) =>
        rows.map((row, rowIndex) =>
          rowIndex === index
            ? {
                ...row,
                quantity: String(limit),
                stockNotice: `Quantity is limited to ${limit}, the stock available for this row.`,
              }
            : row,
        ),
      )
      return
    }

    setDetails((rows) =>
      rows.map((row, rowIndex) =>
        rowIndex === index ? { ...row, quantity: value, stockNotice: null } : row,
      ),
    )
  }

  function onItemChange(index: number, itemIdStr: string) {
    const item = items.find((i) => String(i.id) === itemIdStr)
    const newUnit = item?.unit ?? ''
    setDetails((d) =>
      d.map((r, i) =>
        i === index ? { ...r, itemId: itemIdStr, unit: newUnit, available: null, stockNotice: null } : r,
      ),
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
    if (details.length === 0) e.details = 'Add at least one detail row'
    const requestedByItem = details.reduce<Record<string, number>>((totals, row) => {
      const quantity = Number(row.quantity)
      if (row.itemId && Number.isFinite(quantity) && quantity > 0) {
        totals[row.itemId] = (totals[row.itemId] ?? 0) + quantity
      }
      return totals
    }, {})
    e.rows = details.map((row) => {
      const re: string[] = []
      if (!row.itemId) re.push('Item is required')
      if (!row.unit.trim()) re.push('Unit is required')
      const qty = Number(row.quantity)
      if (row.quantity.trim() === '' || isNaN(qty)) re.push('Quantity is required')
      else if (qty <= 0) re.push('Quantity must be > 0')
      if (form.transactionType === 'Issue' && row.itemId && form.storeId) {
        const allowedTotal = availableTotalForItem(row.itemId)
        if (allowedTotal === null) {
          re.push('Available stock is still loading. Please wait and try again')
        } else if (requestedByItem[row.itemId] > allowedTotal) {
          re.push(`Requested total (${requestedByItem[row.itemId]}) exceeds available stock (${allowedTotal})`)
        }
      }
      return re.join('; ')
    })
    setErrors(e)
    const hasRowError = e.rows.some((r) => r !== '')
    return !e.transactionNo && !e.transactionDate && !e.storeId && !e.details && !hasRowError
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
      if (isEditing && transactionId !== undefined) {
        const updatePayload: UpdateStockTransactionPayload = {
          transactionDate: form.transactionDate || null,
          remarks: form.remarks.trim() || null,
          details: details.map((d) => ({
            id: d.id ?? 0,
            itemId: Number(d.itemId),
            quantity: Number(d.quantity),
            unit: d.unit,
            remarks: d.remarks.trim() || null,
          })),
        }
        const updated = await updateStockTransaction(transactionId, updatePayload)
        applyTransaction(updated)
        setSuccess(`Transaction '${updated.transactionNo}' updated.`)
      } else if (form.transactionType === 'Receive') {
        await createReceiveTransaction(payload)
        setSuccess(`${form.transactionType} transaction '${form.transactionNo}' created.`)
        setForm(initialForm())
        setDetails([emptyDetail()])
        setErrors({ rows: [] })
        setOriginalIssueQuantities({})
      } else {
        await createIssueTransaction(payload)
        setSuccess(`${form.transactionType} transaction '${form.transactionNo}' created.`)
        setForm(initialForm())
        setDetails([emptyDetail()])
        setErrors({ rows: [] })
        setOriginalIssueQuantities({})
      }
    } catch (err) {
      setSubmitError(extractError(err))
    } finally {
      setSubmitting(false)
    }
  }

  if (metaLoading) {
    return <p>Loading form...</p>
  }

  if (metaError) {
    return <div className="alert alert-error">Unable to load items and stores: {metaError}</div>
  }

  if (transactionLoading) {
    return <p>Loading transaction...</p>
  }

  if (transactionError) {
    return <div className="alert alert-error">Unable to load transaction: {transactionError}</div>
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="form-section">
        <h3>{isEditing ? 'Edit Transaction' : 'Header'}</h3>
        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="transaction-no">Transaction No.</label>
            <input
              id="transaction-no"
              required
              value={form.transactionNo}
              onChange={(e) => updateForm('transactionNo', e.target.value)}
              disabled={submitting || isEditing}
            />
            {errors.transactionNo && <div className="field-error">{errors.transactionNo}</div>}
          </div>
          <div className="form-field">
            <label htmlFor="transaction-date">Transaction Date</label>
            <input
              id="transaction-date"
              type="date"
              required
              value={form.transactionDate}
              onChange={(e) => updateForm('transactionDate', e.target.value)}
              disabled={submitting}
            />
            {errors.transactionDate && <div className="field-error">{errors.transactionDate}</div>}
          </div>
          <div className="form-field">
            <label htmlFor="transaction-type">Transaction Type</label>
            <select
              id="transaction-type"
              value={form.transactionType}
              onChange={(e) => updateForm('transactionType', e.target.value as TransactionType)}
              disabled={submitting || isEditing}
            >
              <option value="Receive">Receive</option>
              <option value="Issue">Issue</option>
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="store">Store</label>
            <select
              id="store"
              required
              value={form.storeId}
              onChange={(e) => updateForm('storeId', e.target.value)}
              disabled={submitting || isEditing}
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
            <label htmlFor="transaction-remarks">Remarks</label>
            <input
              id="transaction-remarks"
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
        {errors.details && <div className="field-error">{errors.details}</div>}
        <table className="data-table detail-grid">
          <thead>
            <tr>
              <th>Item</th>
              <th>Date</th>
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
                qty > (availableForRow(index) ?? row.available)
              const rowLimit = form.transactionType === 'Issue' ? availableForRow(index) : null
              return (
                <tr key={index}>
                  <td>
                    <select
                      aria-label={`Item for detail row ${index + 1}`}
                      required
                      value={row.itemId}
                      onChange={(e) => onItemChange(index, e.target.value)}
                      disabled={submitting || (isEditing && row.id !== undefined)}
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
                      aria-label={`Date for detail row ${index + 1}`}
                      type="date"
                      value={form.transactionDate}
                      disabled
                      title="Detail dates use the transaction date."
                    />
                  </td>
                  <td>
                    <input
                      aria-label={`Quantity for detail row ${index + 1}`}
                      type="number"
                      min="0.001"
                      max={rowLimit ?? undefined}
                      step="any"
                      required
                      value={row.quantity}
                      onChange={(e) => onQuantityChange(index, e.target.value)}
                      disabled={submitting}
                    />
                    {exceeds && <div className="field-error">Requested quantity exceeds available stock</div>}
                    {row.stockNotice && <div className="field-warning">{row.stockNotice}</div>}
                  </td>
                  <td>
                    <input
                      aria-label={`Unit for detail row ${index + 1}`}
                      required
                      value={row.unit}
                      onChange={(e) => updateRow(index, 'unit', e.target.value)}
                      disabled={submitting}
                    />
                  </td>
                  <td>
                    <input
                      aria-label={`Remarks for detail row ${index + 1}`}
                      value={row.remarks}
                      onChange={(e) => updateRow(index, 'remarks', e.target.value)}
                      disabled={submitting}
                    />
                  </td>
                  {form.transactionType === 'Issue' && (
                    <td>{row.available !== null ? row.available : 'Loading...'}</td>
                  )}
                  <td>
                    <button
                      type="button"
                      onClick={() => removeRow(index)}
                      disabled={submitting}
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
          {submitting ? 'Saving...' : isEditing ? 'Update Transaction' : 'Save Transaction'}
        </button>
      </div>
    </form>
  )
}
