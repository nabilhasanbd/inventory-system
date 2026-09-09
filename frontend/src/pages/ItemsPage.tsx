import { useEffect, useState } from 'react'
import type { Item, CreateItemPayload, UpdateItemPayload } from '../types/item'
import { getItems, createItem, updateItem, setItemStatus } from '../api/items'
import { extractError } from '../api/errors'
import Modal from '../components/Modal'
import ItemForm from '../components/ItemForm'

export default function ItemsPage() {
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Item | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setItems(await getItems())
    } catch (err) {
      setError(extractError(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  function openCreate() {
    setEditing(null)
    setMessage(null)
    setModalOpen(true)
  }

  function openEdit(item: Item) {
    setEditing(item)
    setMessage(null)
    setModalOpen(true)
  }

  function closeModal() {
    setModalOpen(false)
    setEditing(null)
  }

  async function handleSubmit(payload: CreateItemPayload | UpdateItemPayload) {
    if (editing) {
      await updateItem(editing.id, payload as UpdateItemPayload)
      setMessage(`Item '${payload.itemCode}' updated.`)
    } else {
      await createItem(payload as CreateItemPayload)
      setMessage(`Item '${payload.itemCode}' created.`)
    }
    setError(null)
    closeModal()
    await load()
  }

  async function handleToggle(item: Item) {
    setMessage(null)
    setError(null)
    try {
      await setItemStatus(item.id, !item.isActive)
      setMessage(`Item '${item.itemCode}' ${item.isActive ? 'deactivated' : 'activated'}.`)
      await load()
    } catch (err) {
      setError(extractError(err))
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Item Master</h1>
        <button className="primary" onClick={openCreate}>
          + New Item
        </button>
      </div>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p>Loading items...</p>
      ) : items.length === 0 ? (
        <p>No items found.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Category</th>
              <th>Unit</th>
              <th>Reorder Level</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id}>
                <td>{item.itemCode}</td>
                <td>{item.itemName}</td>
                <td>{item.category ?? '-'}</td>
                <td>{item.unit}</td>
                <td>{item.reorderLevel}</td>
                <td>{item.isActive ? 'Active' : 'Inactive'}</td>
                <td className="actions">
                  <button onClick={() => openEdit(item)}>Edit</button>
                  <button onClick={() => handleToggle(item)}>
                    {item.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <Modal open={modalOpen} title={editing ? 'Edit Item' : 'New Item'} onClose={closeModal}>
        <ItemForm
          mode={editing ? 'edit' : 'create'}
          initialItem={editing ?? undefined}
          onSubmit={handleSubmit}
          onCancel={closeModal}
        />
      </Modal>
    </div>
  )
}
