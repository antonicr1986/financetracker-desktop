namespace FinanceTracker.Desktop.Localization;

/// <summary>
/// Todos los textos de la aplicacion, en español e ingles. Mismas claves y,
/// donde existen, las mismas frases que el diccionario de la web
/// (src/lib/i18n/messages.ts), como hace Android.
///
/// Un test comprueba que los dos idiomas tienen exactamente las mismas claves,
/// y otro que cada {DynamicResource ...} de los XAML existe aqui: olvidar una
/// traduccion no llega a producción.
/// </summary>
public static class Strings
{
    public static readonly IReadOnlyDictionary<string, string> Es = new Dictionary<string, string>
    {
        ["locale.label"] = "Idioma",
        ["locale.es"] = "Español",
        ["locale.en"] = "Inglés",
        ["header.signOut"] = "Salir",
        ["theme.toggle"] = "Cambiar entre tema claro y oscuro",

        ["login.windowTitle"] = "FinanceTracker — Iniciar sesión",
        ["login.title"] = "Iniciar sesión",
        ["login.email"] = "Correo electrónico",
        ["login.emailPlaceholder"] = "tu@correo.com",
        ["login.password"] = "Contraseña",
        ["login.passwordPlaceholder"] = "Tu contraseña",
        ["login.submit"] = "Entrar",
        ["login.or"] = "o",
        ["login.demo"] = "Entrar con la cuenta de demostración",
        ["login.demoHint"] = "Datos de ejemplo compartidos, sin registrarse.",
        ["login.connecting"] = "Conectando… la primera vez del día puede tardar un poco.",

        ["dashboard.greeting"] = "Hola, {0}",
        ["table.month"] = "Mes",
        ["dashboard.reload"] = "Recargar",
        ["dashboard.newTransactionButton"] = "+ Nuevo movimiento",
        ["dashboard.income"] = "Ingresos",
        ["dashboard.expenses"] = "Gastos",
        ["dashboard.balance"] = "Balance",
        ["dashboard.loading"] = "Cargando movimientos… la primera vez del día puede tardar un poco.",
        ["dashboard.retry"] = "Reintentar",
        ["dashboard.emptyTitle"] = "Aún no tienes movimientos",

        ["dialog.title"] = "Nuevo movimiento",
        ["dialog.editTitle"] = "Editar movimiento",
        ["dialog.expense"] = "Gasto",
        ["dialog.income"] = "Ingreso",
        ["dialog.concept"] = "Concepto",
        ["dialog.conceptPlaceholder"] = "Compra del mes",
        ["dialog.amount"] = "Importe (€)",
        ["dialog.date"] = "Fecha",
        ["dialog.category"] = "Categoría",
        ["dialog.cancel"] = "Cancelar",
        ["dialog.save"] = "Guardar",
        ["dialog.delete"] = "Eliminar",
        ["dialog.deleteConfirm"] = "¿Eliminar este movimiento?",
        ["dialog.deleteConfirmBody"] = "«{0}» se eliminará y no se puede deshacer.",

        ["errors.fillCredentials"] = "Introduce tu correo y tu contraseña.",
        ["errors.writeConcept"] = "Escribe un concepto.",
        ["errors.conceptTooLong"] = "El concepto no puede pasar de {0} caracteres.",
        ["errors.invalidAmount"] = "El importe no es un número válido. Usa coma o punto para los decimales.",
        ["errors.amountPositive"] = "El importe debe ser mayor que cero.",
        ["errors.chooseDate"] = "Elige una fecha.",
        ["errors.noExpenseCategories"] = "No tienes categorías de gasto. Crea una desde la web o la app Android.",
        ["errors.noIncomeCategories"] = "No tienes categorías de ingreso. Crea una desde la web o la app Android.",
        ["errors.loadFailed"] = "No se han podido cargar los datos.",
        ["errors.saveFailed"] = "No se ha podido guardar el movimiento.",
        ["errors.deleteFailed"] = "No se ha podido eliminar el movimiento.",
        ["errors.unknown"] = "Algo ha fallado. Inténtalo de nuevo.",

        ["apiError.session_expired"] = "Tu sesión ha caducado. Vuelve a iniciar sesión.",
        ["apiError.invalid_credentials"] = "Correo o contraseña incorrectos.",
        ["apiError.network_error"] = "No se ha podido contactar con el servidor.",
        ["apiError.not_found"] = "Este movimiento ya no existe. Cierra y recarga.",
        ["apiError.category_not_found"] = "La categoría seleccionada no existe.",
        ["apiError.category_type_mismatch"] = "La categoría seleccionada no es del mismo tipo que el movimiento.",
    };

    public static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        ["locale.label"] = "Language",
        ["locale.es"] = "Spanish",
        ["locale.en"] = "English",
        ["header.signOut"] = "Sign out",
        ["theme.toggle"] = "Switch between light and dark theme",

        ["login.windowTitle"] = "FinanceTracker — Sign in",
        ["login.title"] = "Sign in",
        ["login.email"] = "Email address",
        ["login.emailPlaceholder"] = "you@email.com",
        ["login.password"] = "Password",
        ["login.passwordPlaceholder"] = "Your password",
        ["login.submit"] = "Sign in",
        ["login.or"] = "or",
        ["login.demo"] = "Sign in with the demo account",
        ["login.demoHint"] = "Shared sample data, no sign-up needed.",
        ["login.connecting"] = "Connecting… the first time each day can take a while.",

        ["dashboard.greeting"] = "Hi, {0}",
        ["table.month"] = "Month",
        ["dashboard.reload"] = "Reload",
        ["dashboard.newTransactionButton"] = "+ New transaction",
        ["dashboard.income"] = "Income",
        ["dashboard.expenses"] = "Expenses",
        ["dashboard.balance"] = "Balance",
        ["dashboard.loading"] = "Loading transactions… the first time each day can take a while.",
        ["dashboard.retry"] = "Retry",
        ["dashboard.emptyTitle"] = "You have no transactions yet",

        ["dialog.title"] = "New transaction",
        ["dialog.editTitle"] = "Edit transaction",
        ["dialog.expense"] = "Expense",
        ["dialog.income"] = "Income",
        ["dialog.concept"] = "Description",
        ["dialog.conceptPlaceholder"] = "Weekly shop",
        ["dialog.amount"] = "Amount (€)",
        ["dialog.date"] = "Date",
        ["dialog.category"] = "Category",
        ["dialog.cancel"] = "Cancel",
        ["dialog.save"] = "Save",
        ["dialog.delete"] = "Delete",
        ["dialog.deleteConfirm"] = "Delete this transaction?",
        ["dialog.deleteConfirmBody"] = "“{0}” will be deleted and this cannot be undone.",

        ["errors.fillCredentials"] = "Enter your email and your password.",
        ["errors.writeConcept"] = "Enter a description.",
        ["errors.conceptTooLong"] = "The description cannot be longer than {0} characters.",
        ["errors.invalidAmount"] = "The amount is not a valid number. Use a comma or a dot for decimals.",
        ["errors.amountPositive"] = "The amount must be greater than zero.",
        ["errors.chooseDate"] = "Choose a date.",
        ["errors.noExpenseCategories"] = "You have no expense categories. Create one from the web or the Android app.",
        ["errors.noIncomeCategories"] = "You have no income categories. Create one from the web or the Android app.",
        ["errors.loadFailed"] = "Could not load the data.",
        ["errors.saveFailed"] = "Could not save the transaction.",
        ["errors.deleteFailed"] = "Could not delete the transaction.",
        ["errors.unknown"] = "Something went wrong. Please try again.",

        ["apiError.session_expired"] = "Your session has expired. Please sign in again.",
        ["apiError.invalid_credentials"] = "Incorrect email or password.",
        ["apiError.network_error"] = "Could not reach the server.",
        ["apiError.not_found"] = "This transaction no longer exists. Close and reload.",
        ["apiError.category_not_found"] = "The selected category does not exist.",
        ["apiError.category_type_mismatch"] = "The selected category is not the same type as the transaction.",
    };
}
