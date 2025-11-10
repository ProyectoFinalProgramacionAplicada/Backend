using NetTopologySuite.Geometries;

namespace TruekAppAPI.Services
{
    public class GeoService : IGeoService
    {
        // El SRID 4326 es el estándar mundial para Lat/Lng (WGS 84)
        private readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        public Point CreatePoint(double latitude, double longitude)
        {
            // ¡OJO! El estándar GIS es (X, Y) que equivale a (Longitud, Latitud)
            return _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        }
    }
}