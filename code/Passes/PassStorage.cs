namespace FootballSystem.Storages;

using FootballSystem.Models;
using System.Collections.Generic;
using System.Linq;

public class PassStorage
{
    private List<Pass> _passes = new();

    public PassStorage() {}

    public void CreatePass(Pass pass)
    {
        pass.ID = _passes.Count;
        _passes.Add(pass);
    }

    public bool CheckRefundEligibility(int passId)
    {
        Pass? pass = GetPassByID(passId);
        
        return pass != null && pass.CheckRefundEligibility();
    }

    public bool Refund(int passId)
    {
        Pass? pass = GetPassByID(passId);
        
        if (pass == null || pass.IsRefunded)
        {
            return false;
        }

        pass.IsRefunded = true; 
        return true;
    }

    public Pass? GetPassByID(int passId) 
    {
        return _passes.FirstOrDefault(p => p.ID == passId);
    }
}
