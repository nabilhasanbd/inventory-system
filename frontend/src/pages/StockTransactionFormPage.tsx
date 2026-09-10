import { Link, useParams } from 'react-router-dom'
import StockTransactionForm from '../components/StockTransactionForm'

export default function StockTransactionFormPage() {
  const { id } = useParams()
  const parsedId = id ? Number(id) : undefined
  const transactionId = parsedId !== undefined && Number.isInteger(parsedId) && parsedId > 0 ? parsedId : undefined

  return (
    <div className="page">
      <div className="page-header">
        <h1>{transactionId ? 'Edit Stock Transaction' : 'New Stock Transaction'}</h1>
        <Link className="button" to="/stock-transactions">Back to Transactions</Link>
      </div>
      <p className="page-subtitle">{transactionId ? 'Update transaction details.' : 'Create a Receive or Issue transaction.'}</p>
      <StockTransactionForm transactionId={transactionId} />
    </div>
  )
}