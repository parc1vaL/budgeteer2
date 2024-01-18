using System.Diagnostics.Metrics;

namespace Budgeteer.Server;

public class MetricsService
{
    public const string MeterName = "Budgeteer.Server";
    
    private readonly Counter<int> transactionsAdded;
    private readonly Counter<int> transactionsUpdated;
    private readonly Counter<int> transactionsDeleted;
    
    private readonly Counter<int> accountsAdded;
    private readonly Counter<int> accountsUpdated;
    private readonly Counter<int> accountsDeleted;
    
    private readonly Counter<int> categoriesAdded;
    private readonly Counter<int> categoriesUpdated;
    private readonly Counter<int> categoriesDeleted;

    public MetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        this.transactionsAdded = meter.CreateCounter<int>("transactions_added");
        this.transactionsUpdated = meter.CreateCounter<int>("transactions_updated");
        this.transactionsDeleted = meter.CreateCounter<int>("transactions_deleted");
        
        this.accountsAdded = meter.CreateCounter<int>("accounts_added");
        this.accountsUpdated = meter.CreateCounter<int>("accounts_updated");
        this.accountsDeleted = meter.CreateCounter<int>("accounts_deleted");
        
        this.categoriesAdded = meter.CreateCounter<int>("categories_added");
        this.categoriesUpdated = meter.CreateCounter<int>("categories_updated");
        this.categoriesDeleted = meter.CreateCounter<int>("categories_deleted");
    }

    public void TransactionAdded() => this.transactionsAdded.Add(1);
    public void TransactionUpdated() => this.transactionsUpdated.Add(1);
    public void TransactionDeleted() => this.transactionsDeleted.Add(1);

    public void AccountAdded() => this.accountsAdded.Add(1);
    public void AccountUpdated() => this.accountsUpdated.Add(1);
    public void AccountDeleted() => this.accountsDeleted.Add(1);

    public void CategoryAdded() => this.categoriesAdded.Add(1);
    public void CategoryUpdated() => this.categoriesUpdated.Add(1);
    public void CategoryDeleted() => this.categoriesDeleted.Add(1);
}
