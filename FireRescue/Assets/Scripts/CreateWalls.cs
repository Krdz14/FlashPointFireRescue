using UnityEngine;

public class CreateWalls : MonoBehaviour
{
    public GameObject celdaCompleta;
    public GameObject esqSuperiorRight;
    public GameObject esqSuperiorLeft;
    public GameObject esqInferiorRight;
    public GameObject esqInferiorLeft;
    public GameObject ArribaDerechaAbajo;
    public GameObject ArribaIzqAbajo;
    public GameObject ArribaIzqDer;
    public GameObject izqAbajoDer;
    public GameObject paredArriba;
    public GameObject paredAbajo;
    public GameObject paredDerecha;
    public GameObject paredIzquierda;
    public GameObject sinParedes;

    public float cellSize = 8f;

    // arriba | izquierda | abajo | derecha
    public int[,] matrixPOS = new int [6,8] {{1001, 1000, 1100, 1001, 1100, 1001, 1000, 1100},
                                            {0001, 0000, 0110, 0011, 0110, 0011, 0010, 0110},
                                            {0001, 0100, 1001, 1000, 1000, 1100, 1001, 1100},
                                            {0011, 0110, 0011, 0010, 0010, 0110, 0011, 0110},
                                            {1001, 1000, 1000, 1000, 1100, 1001, 1100, 1101},
                                            {0011, 0010, 0010, 0010, 0110, 0011, 0110, 0111}};

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.rotation = Quaternion.identity;
        CreateWallGrid();
        transform.rotation = Quaternion.Euler(0, 180, 180);

    }

    
    void CreateWallGrid()
    {
        int rows = matrixPOS.GetLength(0);
        int cols = matrixPOS.GetLength(1);

        Vector3 startPos = Vector3.zero; // comienza en (0,0,0)

        for (int i = 0; i < rows; i++) // eje z
        {
            for (int j = 0; j < cols; j++) //eje x
            {
                // Posición de la celda
                //Vector3 cellPos = startPos + new Vector3(i * cellSize, 0, -j * cellSize);

                //string pattern = matrixPOS[i, j].ToString("D4"); // ejemplo: "1010"

                int invertedJ = cols - 1 - j;
                
                Vector3 cellPos = startPos + new Vector3(i * cellSize, 0, j * cellSize);

                string pattern = matrixPOS[i, invertedJ].ToString("D4");
                GameObject prefabToSpawn = GetPrefabForPattern(pattern);

                if (prefabToSpawn != null)
                {
                    Instantiate(prefabToSpawn, cellPos, Quaternion.identity, transform);
                }
                else
                {
                    Debug.LogWarning($"No hay prefab asignado para patrón: {pattern}");
                }
            }
        }
    }

    


    GameObject GetPrefabForPattern(string pattern)
    {
        switch (pattern)
        {
            case "1111": return celdaCompleta;
            case "1100": return esqSuperiorLeft;   // arriba + izquierda
            case "0110": return esqInferiorLeft;   // izquierda + abajo
            case "0011": return esqInferiorRight;  // abajo + derecha
            case "1001": return esqSuperiorRight;  // arriba + derecha
            case "1011": return ArribaDerechaAbajo;
            case "1110": return ArribaIzqAbajo;
            case "1101": return ArribaIzqDer;
            case "0111": return izqAbajoDer;
            case "1000": return paredArriba;
            case "0010": return paredAbajo;
            case "0001": return paredDerecha;
            case "0100": return paredIzquierda;
            case "0000": return sinParedes;
            default: return null; // si no hay prefab definido
        }
    }

}
