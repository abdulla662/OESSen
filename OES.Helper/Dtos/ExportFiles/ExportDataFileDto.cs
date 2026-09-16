using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OES.Helper.Dtos.ExportFiles
{
    public class ExportDataFileDto
    {
        public List<long> itemBanksIds { get; set; }
        public long languageId { get; set; }
        public bool withTags { get; set; }
    }
}
