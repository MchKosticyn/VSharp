using System.Diagnostics;
using VSharp.Core;

namespace VSharp.Solver;

using System;
using VSharp;
using Core;

internal class SolverBuilder
{
    private class Path
    {
        internal enum LocationKind
        {
            PointerAddress,
            PointerOffset,
            Value
        }

        private LocationKind _kind = LocationKind.Value;
        private List<fieldId> _structFields = new ();

        internal void AddStructField(fieldId field)
        {
            _structFields.Add(field);
        }

        internal void AddPointerAddress()
        {
            Debug.Assert(_structFields.Count == 0 && _kind == LocationKind.Value);
            _kind = LocationKind.PointerAddress;
        }

        internal void AddPointerOffset()
        {
            Debug.Assert(_structFields.Count == 0 && _kind == LocationKind.Value);
            _kind = LocationKind.PointerOffset;
        }

        internal Type TypeOfLocation
        {
            get
            {
                Debug.Assert(_structFields.Count > 0 || _kind != LocationKind.Value);
                return _kind switch
                {
                    LocationKind.PointerAddress => typeof(TypeUtils.AddressTypeAgent),
                    LocationKind.PointerOffset => typeof(int),
                    LocationKind.Value => _structFields[0].typ,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        internal (address, LocationKind) ToAddress(address address)
        {
            for (var i = _structFields.Count - 1; i >= 0; i--)
            {
                var field = _structFields[i];
                // address = new address.StructField(address, field);
            }

            throw new NotImplementedException();
        }
    }

    private class EncodingException : Exception
    {
        public EncodingException(string message) : base(message)
        {
        }
    }

    private class EncodingResult<IExpr, IBoolExpr>
        where IExpr : IEquatable<IExpr>
    {
        private IExpr _expr;
        private IBoolExpr[] _assumptions;

        private EncodingResult(IExpr expr)
        {
            _expr = expr;
            _assumptions = System.Array.Empty<IBoolExpr>();
        }

        private EncodingResult(IExpr expr, IBoolExpr[] assumptions)
        {
            _expr = expr;
            _assumptions = assumptions;
        }
    }
}
