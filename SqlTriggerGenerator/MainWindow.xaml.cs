using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SqlTriggerGenerator.Annotations;

namespace SqlTriggerGenerator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private string _insertTrigger;
        private string _updateTrigger;
        private string _deleteTrigger;
        private string _totalTrigger;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        public string InputText { get; set; }

        public string InsertTrigger
        {
            get => _insertTrigger;
            set
            {
                _insertTrigger = value;
                OnPropertyChanged(nameof(InsertTrigger));
            }
        }

        public string UpdateTrigger
        {
            get => _updateTrigger;
            set
            {
                _updateTrigger = value;
                OnPropertyChanged(nameof(UpdateTrigger));
            }
        }

        public string DeleteTrigger
        {
            get => _deleteTrigger;
            set
            {
                _deleteTrigger = value;
                OnPropertyChanged(nameof(DeleteTrigger));
            }
        }

        public string TotalTrigger
        {
            get => _totalTrigger;
            set
            {
                _totalTrigger = value;
                OnPropertyChanged(nameof(TotalTrigger));
            }
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            InputText = tb.Text;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //    var t =
            //        "create table TB_NKL_Checkliste\r\n(\r\n Id bigint identity\r\n  constraint PK_TB_NKL_Checkliste\r\n   primary key,\r\n AktivitaetsId int not null\r\n  constraint FK_TB_NKL_Checkliste_TB_NKL_Aktivitaet\r\n   references TB_NKL_Aktivitaet,\r\n NKLId bigint not null\r\n  constraint FK_TB_NKL_Checkliste_TB_NKL_MM\r\n   references TB_NKL_MM,\r\n StartTime datetime,\r\n EndTime datetime,\r\n PlanEndTime datetime,\r\n VerantwortlicherUserId nvarchar(255)\r\n  constraint FK_TB_NKL_Checkliste_TB_User\r\n   references TB_User,\r\n DelegiertAm datetime,\r\n WiedervorlageId int\r\n  constraint FK_TB_NKL_Checkliste_TB_Wiedervorlage\r\n   references TB_Wiedervorlage,\r\n StatusId int\r\n  constraint FK_TB_NKL_Checkliste_TB_Status\r\n   references TB_NKL_Status,\r\n ErledigtVonUserId int,\r\n SystemAbschlussTime datetime\r\n)\r\ngo\r\n\r\n";
            var tableFieldSpecifications = SqlStatementSerializer.ParseSqlStatement(InputText);
            InsertTrigger = TriggerGenerator.GenerateInsertTrigger(tableFieldSpecifications);
            UpdateTrigger = TriggerGenerator.GenerateUpdateTrigger(tableFieldSpecifications);
            DeleteTrigger = TriggerGenerator.GenerateDeleteTrigger(tableFieldSpecifications);
            TotalTrigger = InsertTrigger + " " + DeleteTrigger + " " + UpdateTrigger;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class TriggerGenerator
    {
        private const string ModDateName = "mod_date";
        private const string ModDateType = "datetime";
        private const string ModTypeName = "mod_type";
        private const string ModTypeType = "nvarchar(50)";
        private const string ModByName = "mod_by";
        private const string ModByType = "int";
        private const string NewLine = "\r\n";
        private const string Prefix = "@";
        private const string Postfix = ",";
        private const string WhiteSpace = " ";

        public static string GenerateInsertTrigger(TableStructure tableFieldSpecifications)
        {
            var tn = tableFieldSpecifications.TableName;

            var query =
                $"CREATE TRIGGER [dbo].[{tn}_InsertTrigger]{NewLine}" +
                $"ON [dbo].[{tn}]{NewLine}" +
                $"AFTER INSERT{NewLine}" +
                $"AS{NewLine}" +
                $"BEGIN{NewLine}" +
                $"SET NOCOUNT ON;{NewLine}" +
                $"DECLARE {DeclarationQuery(tableFieldSpecifications)}{NewLine}" +
                $"@{ModDateName} {ModDateType},{NewLine}" +
                $"@{ModTypeName} {ModTypeType},{NewLine}" +
                $"@{ModByName} {ModByType}{NewLine}" +
                $"{SetQuery(tableFieldSpecifications, "inserted")}{NewLine}" +
                $"SET @{ModDateName} = CURRENT_TIMESTAMP{NewLine}" +
                $"SET @{ModTypeName} = 'insert'{NewLine}" +
                $"SET @{ModByName} = (SELECT [dbo].[fn_GetUserIdByHostname]()){NewLine}" +
                $"INSERT INTO [dbo].Audit_{tn}{NewLine}" +
                $"{TableFields(tableFieldSpecifications)}{NewLine}" +
                $"{ModDateName},{NewLine}" +
                $"{ModByName},{NewLine}" +
                $"{ModTypeName}){NewLine}" +
                $"VALUES{NewLine}" +
                $"{FiledValues(tableFieldSpecifications)}{NewLine}" +
                $"@{ModDateName},{NewLine}" +
                $"@{ModByName},{NewLine}" +
                $"@{ModTypeName}){NewLine}" +
                $"END{NewLine}" +
                $"GO{NewLine}";

            return query;
        }

        public static string GenerateUpdateTrigger(TableStructure tableFieldSpecifications)
        {
            var tn = tableFieldSpecifications.TableName;

            var query =
                $"CREATE TRIGGER [dbo].[{tn}_UpdateTrigger]{NewLine}" +
                $"ON [dbo].[{tn}]{NewLine}" +
                $"AFTER UPDATE{NewLine}" +
                $"AS{NewLine}" +
                $"BEGIN{NewLine}" +
                $"SET NOCOUNT ON;{NewLine}" +
                $"DECLARE {DeclarationQuery(tableFieldSpecifications)}{NewLine}" +
                $"@{ModDateName} {ModDateType},{NewLine}" +
                $"@{ModTypeName} {ModTypeType},{NewLine}" +
                $"@{ModByName} {ModByType}{NewLine}" +
                "-- ##############################" +
                $"-- SET primary key value manually.{NewLine}" +
                $"--SET @Id = (select d.id{NewLine}" +
                $"--           from deleted d{NewLine}" +
                $"--                join inserted i on (i.ID = d.ID)){NewLine}" +
                $"--########################{NewLine}" +
                $"{SetQuery(tableFieldSpecifications, "inserted")}{NewLine}" +
                $"SET @{ModDateName} = CURRENT_TIMESTAMP{NewLine}" +
                $"SET @{ModTypeName} = 'update'{NewLine}" +
                $"SET @{ModByName} = (SELECT [dbo].[fn_GetUserIdByHostname]()){NewLine}" +
                $"INSERT INTO [dbo].Audit_{tn}{NewLine}" +
                $"{TableFields(tableFieldSpecifications)}{NewLine}" +
                $"{ModDateName},{NewLine}" +
                $"{ModByName},{NewLine}" +
                $"{ModTypeName}){NewLine}" +
                $"VALUES{NewLine}" +
                $"{FiledValues(tableFieldSpecifications)}{NewLine}" +
                $"@{ModDateName},{NewLine}" +
                $"@{ModByName},{NewLine}" +
                $"@{ModTypeName}){NewLine}" +
                $"END{NewLine}" +
                $"GO{NewLine}";

            return query;
        }

        public static string GenerateDeleteTrigger(TableStructure tableFieldSpecifications)
        {
            var tn = tableFieldSpecifications.TableName;

            var query =
                $"CREATE TRIGGER [dbo].[{tn}_DeleteTrigger]{NewLine}" +
                $"ON [dbo].[{tn}]{NewLine}" +
                $"AFTER DELETE{NewLine}" +
                $"AS{NewLine}" +
                $"BEGIN{NewLine}" +
                $"SET NOCOUNT ON;{NewLine}" +
                $"DECLARE {DeclarationQuery(tableFieldSpecifications)}{NewLine}" +
                $"@{ModDateName} {ModDateType},{NewLine}" +
                $"@{ModTypeName} {ModTypeType},{NewLine}" +
                $"@{ModByName} {ModByType}{NewLine}" +
                $"{SetQuery(tableFieldSpecifications, "deleted")}{NewLine}" +
                $"SET @{ModDateName} = CURRENT_TIMESTAMP{NewLine}" +
                $"SET @{ModTypeName} = 'delete'{NewLine}" +
                $"SET @{ModByName} = (SELECT [dbo].[fn_GetUserIdByHostname]()){NewLine}" +
                $"INSERT INTO [dbo].Audit_{tn}{NewLine}" +
                $"{TableFields(tableFieldSpecifications)}{NewLine}" +
                $"{ModDateName},{NewLine}" +
                $"{ModByName},{NewLine}" +
                $"{ModTypeName}){NewLine}" +
                $"VALUES{NewLine}" +
                $"{FiledValues(tableFieldSpecifications)}{NewLine}" +
                $"@{ModDateName},{NewLine}" +
                $"@{ModByName},{NewLine}" +
                $"@{ModTypeName}){NewLine}" +
                $"END{NewLine}" +
                $"GO{NewLine}";

            return query;
        }

        private static string FiledValues(TableStructure tableFieldSpecifications)
        {
            var ts = tableFieldSpecifications.TableFieldSpecifications;
            var value = "";

            foreach (var spec in ts)
            {
                var q = $"{Prefix}{spec.FieldName} {Postfix}";
                value += q;
            }

            return $"({value}";
        }

        private static string TableFields(TableStructure tableFieldSpecifications)
        {
            var ts = tableFieldSpecifications.TableFieldSpecifications;
            var value = "";

            foreach (var spec in ts)
            {
                var q = $"{spec.FieldName} {Postfix}";
                value += q;
            }

            return $"({value}";
        }

        private static string SetQuery(TableStructure tableFieldSpecifications, string actionType)
        {
            var ts = tableFieldSpecifications.TableFieldSpecifications;
            var value = "";


            foreach (var spec in ts)
            {
                var q = $"SET @{spec.FieldName} = (SELECT {spec.FieldName} FROM {actionType}){WhiteSpace}";
                value += q;
            }

            return value;
        }

        private static string DeclarationQuery(TableStructure tableFieldSpecifications)
        {
            var ts = tableFieldSpecifications.TableFieldSpecifications;
            var value = "";

            foreach (var spec in ts)
            {
                var q = $"{Prefix}{spec.FieldName} {spec.FieldType}{Postfix}";
                value += q;
            }

            return value;
        }
    }

    public class SqlStatementSerializer
    {
        public static TableStructure SerializeSqlStatement(string input)
        {
            return new TableStructure();
        }

        public static TableStructure ParseSqlStatement(string inputText)
        {
            var text = inputText.Replace("identity", "");
            text = text.Replace("not null", "");

            var i = text.IndexOf("(");
            text = text.Remove(i, 1);
            text = text.Insert(i, "??");
            var firstSplit = text.Split("??").ToList();

            var header = firstSplit[0];
            var body = firstSplit[1];

            var tableName = ParseTableName(header);
            var rowSpecifications = ParseFieldSpecifications(body);

            var item = new TableStructure
            {
                TableName = tableName,
                TableFieldSpecifications = rowSpecifications
            };

            return item;
        }

        private static List<TableFieldSpecification> ParseFieldSpecifications(string body)
        {
            var dummy = new List<string>();
            var list = new List<TableFieldSpecification>();

            var separatedString = body.Split(",");

            foreach (var sp in separatedString)
            {
                var line = sp.Split("\r\n").ToList();

                foreach (var newString in line)
                {
                    var contains = false;

                    foreach (var keyWord in SqlTypesList)
                    {
                        contains = newString.Contains(keyWord) && !newString.Contains("constraint");

                        if (contains) break;
                    }

                    if (contains) dummy.Add(newString);
                }
            }

            foreach (var d in dummy)
            {
                var val = d.Trim();
                var stringList = val.Split(" ");
                var value = new List<string>();
                foreach (var s in stringList)
                    if (!string.IsNullOrWhiteSpace(s))
                        value.Add(s);

                var item = new TableFieldSpecification
                {
                    FieldName = value[0].Trim(),
                    FieldType = value[1].Trim()
                };

                list.Add(item);
            }

            return list;
        }

        private static string ParseTableName(string header)
        {
            var name = header.Replace("create table ", "");
            return name.Replace("\r\n", "");
        }

        private static readonly List<string> SqlTypesList = new List<string>
        {
            "bigint",
            "bit",
            "decimal",
            "int",
            "money",
            "float",
            "date",
            "datetime",
            "datetime2",
            "char",
            "text",
            "nchar",
            "ntext",
            "nvarchar",
            "varchar"
        };

        public static string ParseSqlStatement2(string s)
        {
            var test = s;
            var i = test.IndexOf("(");
            test = test.Remove(i, 1);
            test = test.Insert(i, ",");
            test = test.Replace("create table", "");
            test = test.Replace("identity", "");
            var l = test.Split(",");

            return "";
        }
    }

    public class TableStructure
    {
        public string TableName { get; set; }

        public List<TableFieldSpecification> TableFieldSpecifications { get; set; }
    }


    public class TableFieldSpecification
    {
        public string FieldName { get; set; }
        public string FieldType { get; set; }
    }
}