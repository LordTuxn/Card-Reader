using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CardReaderOne.Models {

    public class EmployeeRecord {

        public EmployeeRecord() {
        }

        public EmployeeRecord(string cardNr) {
            this.CardNumber = cardNr;
        }

        public string CardNumber { get; set; }

        public string DisplayName { get; set; }

        public string EMail { get; set; }

        public int EmployeeNumber { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public override string ToString() {
            return $"{CardNumber} {DisplayName} {EMail} {EmployeeNumber} {Firstname} {Lastname} {ValidFrom.ToShortDateString()} {ValidTo.ToShortDateString()} ";
        }
    }
}