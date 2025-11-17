namespace DocumentChecker.Models
{
    /// <summary>
    /// Определения стандартных полей (11 обязательных)
    /// </summary>
    public static class FieldDefinition
    {
        public static readonly Field[] DefaultFields = new Field[]
        {
            new Field { Id = 1, Name = "Наименование изделия" },
            new Field { Id = 2, Name = "Модель/модификация" },
            new Field { Id = 3, Name = "Наименование производителя" },
            new Field { Id = 4, Name = "Страна производителя" },
            new Field { Id = 5, Name = "Наименование заявителя" },
            new Field { Id = 6, Name = "Страна заявителя" },
            new Field { Id = 7, Name = "Адрес производителя" },
            new Field { Id = 8, Name = "Адрес заявителя" },
            new Field { Id = 9, Name = "Наименование испытательной лаборатории" },
            new Field { Id = 10, Name = "Регистрационный номер/код изделия" },
            new Field { Id = 11, Name = "Дополнительное поле" }
        };
    }
}
