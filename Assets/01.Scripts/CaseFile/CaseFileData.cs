using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CaseFile", menuName = "CaseFile/Case File Data")]
public class CaseFileData : ScriptableObject
{
    public string caseID;
    public string caseName;
    public List<CaseFileItemData> items = new List<CaseFileItemData>();
}
