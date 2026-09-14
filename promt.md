Now perform a complete audit of the Inventory Stock Management System.

Do not add new features.

Review the entire application for these business rules:

1. Receive increases stock correctly.
2. Issue decreases stock correctly.
3. Negative stock is impossible.
4. Client-side Issue validation exists.
5. Server-side Issue validation exists.
6. Multiple details save atomically.
7. Transaction rollback works.
8. Transaction update correctly handles:
   - new details
   - modified details
   - deleted details
9. Stock is correctly adjusted when details are added/modified/deleted.
10. Deleting a transaction reverses its stock effect.
11. Concurrent Issue requests cannot create negative stock.
12. Current Stock matches transaction history.
13. Opening stock is calculated correctly.
14. Receive/Issue totals are correct.
15. Closing stock is correct.
16. Report filtering works correctly.
17. No unnecessary EF entities are exposed through APIs.
18. Validation and error handling are appropriate.

Inspect both backend and frontend.

Fix only real issues you find.

Afterward provide a concise audit report listing:
- problems found
- fixes made
- remaining risks

always give me simple and non repeatative code