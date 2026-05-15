using UnityEngine;

namespace MobilOfl.UI
{
    public static class SchoolLocationUtility
    {
        private const float ImportedOriginX = 920f;
        private const float ImportedOriginY = 5.08f;
        private const float ImportedOriginZ = -585f;
        private const float ImportedZToWorldX = 0.55f;
        private const float ImportedXToWorldZ = 4f;
        private const float PrototypeGroundY = 0.05f;

        public static bool IsImportedMapPosition(Vector3 position)
        {
            return position.x > 850f && position.z < -450f;
        }

        public static Vector3 PrototypeToWorldPosition(Vector3 prototypePosition, Vector3 contextPosition)
        {
            if (!IsImportedMapPosition(contextPosition) || IsImportedMapPosition(prototypePosition))
            {
                return prototypePosition;
            }

            return new Vector3(
                ImportedOriginX + prototypePosition.z * ImportedZToWorldX,
                ImportedOriginY + prototypePosition.y - PrototypeGroundY,
                ImportedOriginZ + prototypePosition.x * ImportedXToWorldZ);
        }

        public static Vector3 NormalizeToPrototypeSpace(Vector3 position)
        {
            if (IsImportedMapPosition(position))
            {
                return new Vector3(
                    (position.z - ImportedOriginZ) / ImportedXToWorldZ,
                    position.y,
                    (position.x - ImportedOriginX) / ImportedZToWorldX);
            }

            return position;
        }

        public static string GetZoneTitle(Vector3 position)
        {
            position = NormalizeToPrototypeSpace(position);

            if (position.z > 18f)
            {
                return "AVLU";
            }

            if (position.x < -10f && position.z > 7f)
            {
                return "ARSIV";
            }

            if (position.x > 10f && position.z > 7f)
            {
                return "KANTIN";
            }

            if (position.x < -10f)
            {
                return "LAB KANADI";
            }

            if (position.x > 10f)
            {
                return "SPOR KANADI";
            }

            if (position.x < -4f && position.z < 1f)
            {
                return "SINIF BLOGU";
            }

            if (position.x > 4f && position.z < 4f)
            {
                return "KUTUPHANE";
            }

            if (position.x < -4f && position.z > 7f)
            {
                return "GUVENLIK ODASI";
            }

            if (position.x > 4f && position.z > 7f)
            {
                return "OGRETMENLER ODASI";
            }

            if (position.z < -4.5f)
            {
                return "VAKA MASASI";
            }

            return "ANA KORIDOR";
        }

        public static string GetZoneSubtitle(Vector3 position)
        {
            position = NormalizeToPrototypeSpace(position);

            if (position.z > 18f)
            {
                return "Acik alani tara, takimla gorus hatti daha genis.";
            }

            if (position.x < -10f && position.z > 7f)
            {
                return "Eski kayitlar ve erisim izleri bu odada saklaniyor.";
            }

            if (position.x > 10f && position.z > 7f)
            {
                return "Gec saat taniklari ve harcama izleri bu tarafta.";
            }

            if (position.x < -10f)
            {
                return "Teknik ekipman ve yan giris ihtimalleri burada.";
            }

            if (position.x > 10f)
            {
                return "Sakin gorunuyor ama yan kanat gecisleri acik.";
            }

            if (position.x < -4f && position.z < 1f)
            {
                return "Ogrenci hareketi yogun. Fiziksel deliller kolay gizlenir.";
            }

            if (position.x > 4f && position.z < 4f)
            {
                return "Notlar, kagitlar ve tanik ifadesi icin kritik alan.";
            }

            if (position.x < -4f && position.z > 7f)
            {
                return "Kamera kayitlari ve gece hareketleri burada izlenir.";
            }

            if (position.x > 4f && position.z > 7f)
            {
                return "Personel erisimi ve dolap anahtarlari bu bolgede.";
            }

            if (position.z < -4.5f)
            {
                return "Toplanan delilleri dosyadan karsilastirmak icin iyi nokta.";
            }

            return "Koridor merkezinden tum odalara hizli ulasim var.";
        }
    }
}
