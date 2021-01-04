using CardReaderOne.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.DataProcess {

    public interface IDataProcessor {

        Task ProcessAsync(EmployeeRecord employee);
    }
}