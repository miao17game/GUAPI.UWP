﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Douban.UWP.Core.Models.ListModel {
    public class MovieItem : ListItemBase {

        public IList<string> Directors { get; set; }

        public IList<string> Actors { get; set; }

    }
}