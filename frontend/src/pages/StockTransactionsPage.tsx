import StockTransactionForm from '../components/StockTransactionForm'
import { useParams } from 'react-router-dom'

export default function StockTransactionsPage() {
  const { id } = useParams()
  const parsedId = id ? Number(id) : undefined
  const transactionId = parsedId !== undefined && Number.isInteger(parsedId) && parsedId > 0 ? parsedId : undefined

  return (
    <div className="page">
      <h1>{transactionId ? 'Edit Stock Transaction' : 'Stock Transactions'}</h1>
      <p className="page-subtitle">
        {transactionId ? 'Update transaction details.' : 'Create a Receive or Issue transaction.'}
      </p>
      <StockTransactionForm transactionId={transactionId} />
    </div>
  )
}
