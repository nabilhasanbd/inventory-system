import { NavLink, Outlet } from 'react-router-dom'

const navItems = [
  { to: '/items', label: 'Item Master' },
  { to: '/stores', label: 'Store Master' },
  { to: '/stock-transactions', label: 'Stock Transactions' },
  { to: '/stock-reports', label: 'Stock Reports' },
  { to: '/transaction-reports', label: 'Transaction Reports' },
]

export default function MainLayout() {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <h2>Inventory System</h2>
        <nav>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => (isActive ? 'active' : '')}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
