Now improve the Issue transaction UI.

For every Issue detail row:

1. Display current available stock.
2. Validate requested quantity against available stock.
3. Prevent the user from entering a quantity greater than available stock.
4. Show a clear validation message.
5. Validate all rows before submitting.

Important:
This is only client-side validation.

The backend server-side validation must remain the final authority.

Do not modify backend business rules unless there is a bug.