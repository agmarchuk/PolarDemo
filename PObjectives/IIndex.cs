using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PObjectives
{
    public interface IIndex
    {
        // Построение индекса по опорной таблице
        void Load();
        // Ресширение индекса при динамическом добавлении входа в опорную таблицу
        void AddEntry(PaEntry ent);
        // Закрытие индекса
        void Close();
    }
}
