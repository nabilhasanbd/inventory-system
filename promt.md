Now implement UPDATE for stock transactions.

The update must support:

- existing details modified
- new details added
- existing details deleted

Example:

Existing database details:
- Laptop qty 10
- Keyboard qty 20
- Mouse qty 15

Updated request:
- Laptop qty 15
- Keyboard qty 20
- Monitor qty 5

Expected result:
- Laptop -> modified
- Keyboard -> unchanged
- Mouse -> deleted
- Monitor -> inserted

The backend must identify these correctly using detail IDs.

Most importantly, stock must remain correct.

For Receive:
- changing 10 to 15 increases stock by 5
- changing 15 to 10 decreases stock by 5
- deleting a received detail reverses its stock effect
- adding a new received detail increases stock

For Issue:
- changing the issue quantity must adjust stock using the difference
- increasing an issue quantity must verify additional available stock
- decreasing an issue quantity must restore the difference
- deleting an issue detail must restore the issued quantity
- adding a new issue detail must validate available stock

The entire update must execute in one database transaction.

If any validation fails, rollback all changes.

Do not implement delete transaction yet.

provide simple and non repeatative code 
Build and test carefully.





