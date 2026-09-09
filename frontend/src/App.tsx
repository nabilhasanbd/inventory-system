import { Routes, Route, Navigate } from 'react-router-dom'
import MainLayout from './layouts/MainLayout'
import ItemsPage from './pages/ItemsPage'
import StoresPage from './pages/StoresPage'
import StockTransactionsPage from './pages/StockTransactionsPage'
import StockReportsPage from './pages/StockReportsPage'
import TransactionReportsPage from './pages/TransactionReportsPage'
import NotFoundPage from './pages/NotFoundPage'

export default function App() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route path="/" element={<Navigate to="/items" replace />} />
        <Route path="/items" element={<ItemsPage />} />
        <Route path="/stores" element={<StoresPage />} />
        <Route path="/stock-transactions" element={<StockTransactionsPage />} />
        <Route path="/stock-transactions/:id/edit" element={<StockTransactionsPage />} />
        <Route path="/stock-reports" element={<StockReportsPage />} />
        <Route path="/transaction-reports" element={<TransactionReportsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
