using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpolisShared.Resource
{
    public static class Messages
    {
        public static string IndexSqlError_Null = "ERR: Lauka '{0}' vērtība nevar būt tukša!";
        public static string IndexSqlError_Unique = "ERR: Lauka '{0}' vērtībai jābūt unikālai, bet tāda jau pastāv citam ierakstam!";

        public static string IndexCreateConfirmFailure = "ERR: Notikusi kļūda ieraksta saglabāšanā!";
        public static string IndexCreateConfirmSuccess = "Ieraksts veiksmīgi saglabāts!";

        public static string IndexEditConfirmFailure = "ERR: Notikusi kļūda ieraksta izmaiņu saglabāšanā!";
        public static string IndexEditConfirmSuccess = "Ieraksta izmaiņas veiksmīgi saglabātas!";

        public static string IndexDeleteConfirmMessage = "Vai tiešām vēlaties dzēst ierakstu?";
        public static string IndexDeleteConfirmFailure = "ERR: Notikusi kļūda ieraksta dzēšanas laikā!";
        public static string IndexDeleteConfirmSuccess = "Ieraksts veiksmīgi dzēsts!";
        
        public static string IndexValidateField_Default = "Lauks '{0}' neatbilst validācijas nosacījumiem!";
        public static string IndexValidateField_Required = "Lauks '{0}' ir obligāts!";
        public static string IndexValidateField_Required_ListBox = "Sarakstā '{0}' nepieciešams izvēlēties vismaz vienu vērtību!";
        public static string IndexValidateField_TextMinLength = "Lauka '{0}' vērtībai jābūt vismaz {1} simboli!";
        public static string IndexValidateField_TextMaxLength = "Lauka '{0}' atļautais garums ir {1} simboli!";
        public static string IndexValidateField_TextRegex = "Lauka '{0}' vērtība neatbilst noteiktajam formātam!";

        public static string ControllerCreateDenied = "ERR: Piekļuve ir liegta.";
        public static string ControllerReadDenied = "ERR: Piekļuve ir liegta.";
        public static string ControllerEditDenied = "ERR: Piekļuve ir liegta.";
        public static string ControllerDeleteDenied = "ERR: Piekļuve ir liegta.";

        public static string DeleteDialogTitle = "Ieraksta dzēšana";
        public static string DeleteDialogMessage = "Vai tiešām vēlaties dzēst ierakstu?";

        public static string ReturnDialogTitle = "Uzmanību!";
        public static string ReturnDialogMessage = "Vai tiešām vēlaties atstāt šo formu, nesaglabājot izmaiņas?";
        public static string ReturnDialogFieldChanged = "Modificēta lauka '{Name}' vērtība no '{OldValue}' uz '{NewValue}'";
        public static string ReturnDialogGridAdded = "Sarakstā '{Name}' pievienoti {CountAdded} jauni ieraksti";
        public static string ReturnDialogGridDeleted = "Sarakstā '{Name}' dzēsti {CountDeleted} ieraksti";
        public static string ReturnDialogGridChanged = "Sarakstā '{Name}' modificēti {CountChanged} ieraksti";

    }
}
