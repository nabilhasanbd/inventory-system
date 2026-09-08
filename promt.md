

Now implement Stock Transaction creation for RECEIVE transactions only.

A transaction contains:

Header:
- TransactionNo
- TransactionDate
- TransactionType
- StoreId
- Remarks

Details:
- ItemId
- Quantity
- Unit
- Remarks

Requirements:

1. One transaction can contain multiple details.
2. TransactionNo must be unique.
3. At least one detail is required.
4. Quantity must be greater than zero.
5. Item must exist and be active.
6. Store must exist and be active.
7. The entire operation must execute inside one database transaction.
8. Create the transaction header.
9. Create all transaction details.
10. Increase StockBalance for every detail.
11. If any operation fails, rollback everything.

Do not implement Issue yet.

Use request/response DTOs.

Do not put the transaction processing logic inside the controller.

Build and test the project.