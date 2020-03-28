
using System;
using Contensive.Models.Db;

namespace aoEmailBounceProcess {
    namespace Db.Models {
        public class BlankDbModel : DbBaseModel {
            /// <summary>
            /// table definition
            /// </summary>
            public static readonly DbBaseTableMetadataModel tableMetadata = new DbBaseTableMetadataModel("blank", "blank", "default", false);
            //
            // -- sample model properties. Each property name matches a database field, and the type matches the Contensive use case.
            // -- These properties will save the default value for the type if not initialized.
            //
            public string sampleDbFieldText { get; set; }
            public int sampleDbFieldInteger { get; set; }
            public bool sampleDbFieldBoolean { get; set; }
            public DateTime sampleDbFieldDate { get; set; }
            public double sampleDbFieldNumber { get; set; }
            //
            // -- sample model file properties with .filename and .content properties.
            //
            public FieldTypeCSSFile sampleDbFieldCssFile { get; set; }
            public FieldTypeHTMLFile sampleDbFielHtmlFile { get; set; }
            public FieldTypeJavascriptFile sampleDbFieldJavascriptFile { get; set; }
            public FieldTypeTextFile sampleDbFieldTextFile { get; set; }
            //
            // -- sample model nullable properties. These properties will save null if not initialized.
            //
            public int? sampleDbFieldNullableInteger { get; set; }
            public bool? sampleDbFieldNullableBoolean { get; set; }
            public DateTime? sampleDbFieldNullableDate { get; set; }
            public double? sampleDbFieldNullableNumber { get; set; }
        }
    }
}
