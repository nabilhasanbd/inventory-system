import StockTransactionForm from '../components/StockTransactionForm'

export default function StockTransactionsPage() {
  return (
    <div className="page">
      <h1>Stock Transactions</h1>
      <p className="page-subtitle">Create a Receive or Issue transaction.</p>
      <StockTransactionForm />
    </div>
  )
}
