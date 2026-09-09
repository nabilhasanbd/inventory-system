import { useEffect, useState } from 'react'
import type { Store, CreateStorePayload, UpdateStorePayload } from '../types/store'
import { getStores, createStore, updateStore, setStoreStatus } from '../api/stores'
import { extractError } from '../api/errors'
import Modal from '../components/Modal'
import StoreForm from '../components/StoreForm'

export default function StoresPage() {
  const [stores, setStores] = useState<Store[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Store | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setStores(await getStores())
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

  function openEdit(store: Store) {
    setEditing(store)
    setMessage(null)
    setModalOpen(true)
  }

  function closeModal() {
    setModalOpen(false)
    setEditing(null)
  }

  async function handleSubmit(payload: CreateStorePayload | UpdateStorePayload) {
    if (editing) {
      await updateStore(editing.id, payload as UpdateStorePayload)
      setMessage(`Store '${payload.code}' updated.`)
    } else {
      await createStore(payload as CreateStorePayload)
      setMessage(`Store '${payload.code}' created.`)
    }
    setError(null)
    closeModal()
    await load()
  }

  async function handleToggle(store: Store) {
    setMessage(null)
    setError(null)
    try {
      await setStoreStatus(store.id, !store.isActive)
      setMessage(`Store '${store.code}' ${store.isActive ? 'deactivated' : 'activated'}.`)
      await load()
    } catch (err) {
      setError(extractError(err))
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Store Master</h1>
        <button className="primary" onClick={openCreate}>
          + New Store
        </button>
      </div>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <p>Loading stores...</p>
      ) : stores.length === 0 ? (
        <p>No stores found.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {stores.map((store) => (
              <tr key={store.id}>
                <td>{store.code}</td>
                <td>{store.name}</td>
                <td>{store.isActive ? 'Active' : 'Inactive'}</td>
                <td className="actions">
                  <button onClick={() => openEdit(store)}>Edit</button>
                  <button onClick={() => handleToggle(store)}>
                    {store.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <Modal open={modalOpen} title={editing ? 'Edit Store' : 'New Store'} onClose={closeModal}>
        <StoreForm
          mode={editing ? 'edit' : 'create'}
          initialStore={editing ?? undefined}
          onSubmit={handleSubmit}
          onCancel={closeModal}
        />
      </Modal>
    </div>
  )
}
