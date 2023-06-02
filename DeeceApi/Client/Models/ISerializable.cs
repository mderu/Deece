using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeeceApi.Client.Models
{
    public interface ISerializable<T>
    {
        T FromBytes(byte[] bytes);
        byte[] ToBytes(T obj);
    }
}
