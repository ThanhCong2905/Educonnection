using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class BaoTriWorker(IServiceScopeFactory scopes,ILogger<BaoTriWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(1));
        while(!stoppingToken.IsCancellationRequested) {
            try {using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<KetNoiService>().XuLyHetHanYeuCauAsync();}
            catch(Exception ex) when(!stoppingToken.IsCancellationRequested) {logger.LogWarning(ex,"Unable to expire connection requests.");}
            try {if(!await timer.WaitForNextTickAsync(stoppingToken))break;}catch(OperationCanceledException){break;}
        }
    }
}
