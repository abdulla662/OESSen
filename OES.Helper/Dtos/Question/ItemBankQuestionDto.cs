using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OES.Helper.Dtos.Question
{
    public class ItemBankQuestionDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Body { get; set; }
        public string Type { get; set; }
        public long ItemBankId { get; set; }
    }
}
