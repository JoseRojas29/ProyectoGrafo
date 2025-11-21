namespace ArbolGenealogicoWPF
{
    public class GridCoord
    {
        public int Row { get; set; }   // Nivel generacional (diferencia de nivel padre-hijo)
        public int Col { get; set; }   // Posición horizontal (relación de igual hermanos y pareja)
    }
}