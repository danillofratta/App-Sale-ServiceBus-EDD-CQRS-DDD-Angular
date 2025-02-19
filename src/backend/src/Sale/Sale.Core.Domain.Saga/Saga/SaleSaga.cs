﻿using Microsoft.Extensions.Logging;
using Sale.Core.Domain.Saga.Commands;
using Sale.Core.Domain.Saga.Events;
using Sale.Core.Domain.Service;

namespace Sale.Core.Domain.Saga
{
    public class SaleSaga : Saga<SaleSagaData>,
        IAmStartedByMessages<SaleCreatedEvent>,
        IHandleMessages<StockConfirmedEvent>,
        IHandleMessages<StockInsufficientEvent>,
        IHandleMessages<PaymentConfirmedEvent>,
        IHandleMessages<PaymentFailEvent>
    {
        private readonly ILogger<SaleSaga> _logger;
        private readonly SaleStockConfirmedService _saleStockConfirmedService;
        private readonly SaleStockInsufficientService _saleStockInsufficientService;
        private readonly SalePaymentFailedService _SalePaymentFailedService;
        private readonly SalePaymentConfirmedService _SalePaymentConfirmedService;

        public SaleSaga(
            ILogger<SaleSaga> logger,
            SaleStockConfirmedService saleStockConfirmedService,
            SaleStockInsufficientService saleStockInsufficientService,
            SalePaymentFailedService SalePaymentFailedService,
            SalePaymentConfirmedService SalePaymentConfirmedService
            )
        {
            _logger = logger;
            _saleStockConfirmedService = saleStockConfirmedService;
            _saleStockInsufficientService = saleStockInsufficientService;
            _SalePaymentFailedService = SalePaymentFailedService;
            _SalePaymentConfirmedService = SalePaymentConfirmedService;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SaleSagaData> mapper)
        {
            mapper.ConfigureMapping<SaleCreatedEvent>(message => message.SaleId)
            .ToSaga(sagaData => sagaData.SaleId);
            mapper.ConfigureMapping<StockConfirmedEvent>(message => message.SaleId)
                .ToSaga(sagaData => sagaData.SaleId);
            mapper.ConfigureMapping<StockInsufficientEvent>(message => message.SaleId)
                .ToSaga(sagaData => sagaData.SaleId);
            mapper.ConfigureMapping<PaymentConfirmedEvent>(message => message.SaleId)
                .ToSaga(sagaData => sagaData.SaleId);
            mapper.ConfigureMapping<PaymentFailEvent>(message => message.SaleId)
                .ToSaga(sagaData => sagaData.SaleId);

            //mapper.MapSaga(saga => saga.SaleId)
            // .ToMessage<SaleCreatedEvent>(msg => msg.SaleId)
            // .ToMessage<StockConfirmedEvent>(msg => msg.SaleId)
            // .ToMessage<StockInsufficientEvent>(msg => msg.SaleId)
            // .ToMessage<PaymentConfirmedEvent>(msg => msg.SaleId)
            // .ToMessage<PaymentFailEvent>(msg => msg.SaleId);
        }

        public async Task Handle(SaleCreatedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Iniciando saga para venda {SaleId}. Verificando estoque...", message.SaleId);

            try
            {
                await context.Send("StockSagaEndpoint", new ReserveStockCommand
                {
                    SaleId = message.SaleId,
                    SaleItens = message.SaleItens
                });

                _logger.LogDebug("Comando ReserveStockCommand enviado para venda {SaleId}", message.SaleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar SaleCreatedEvent para venda {SaleId}", message.SaleId);
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task Handle(StockConfirmedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Estoque confirmado para venda {SaleId}. Processando pagamento...", message.SaleId);

            try
            {
                if (Data.IsStockConfirmed)
                {
                    _logger.LogWarning("Stock already confirmed for sale {SaleId}", message.SaleId);
                    return;
                }


                Data.IsStockConfirmed = true;
                await _saleStockConfirmedService.Process(message.SaleId);

                await context.Send("PaymentSagaEndpoint", new ProcessPaymentCommand
                {
                    SaleId = message.SaleId,
                    Valor = message.Total
                });


                _logger.LogDebug("Comando ProcessPaymentCommand enviado para venda {SaleId}", message.SaleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar StockConfirmedEvent para venda {SaleId}", message.SaleId);
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task Handle(StockInsufficientEvent message, IMessageHandlerContext context)
        {
            _logger.LogWarning("Estoque insuficiente para venda {SaleId}. Cancelando venda...", message.SaleId);

            try
            {
                //await context.Publish(new SaleCancelledEvent { SaleId = message.SaleId });                
                await _saleStockInsufficientService.Process(message.SaleId);                
                MarkAsComplete();

                _logger.LogInformation("Venda {SaleId} cancelada por falta de estoque", message.SaleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar StockInsufficientEvent para venda {SaleId}", message.SaleId);
                throw;
            }

            await Task.CompletedTask;            
        }

        public async Task Handle(PaymentConfirmedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Pagamento confirmado para venda {SaleId}. Finalizando venda...", message.SaleId);

            try
            {
                //await context.Publish(new SaleFinishedEvent { SaleId = message.SaleId });
                await _SalePaymentConfirmedService.Process(message.SaleId);
                MarkAsComplete();

                _logger.LogInformation("Venda {SaleId} finalizada com sucesso", message.SaleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar PaymentConfirmedEvent para venda {SaleId}", message.SaleId);
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task Handle(PaymentFailEvent message, IMessageHandlerContext context)
        {
            _logger.LogWarning("Falha no pagamento para venda {SaleId}. Cancelando venda...", message.SaleId);

            try
            {
                //await context.Publish(new SaleCancelledEvent { SaleId = message.SaleId });
                await _SalePaymentFailedService.Process(message.SaleId);
                MarkAsComplete();

                _logger.LogInformation("Venda {SaleId} cancelada por falha no pagamento", message.SaleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar PaymentFailEvent para venda {SaleId}", message.SaleId);
                throw;
            }

            await Task.CompletedTask;
        }
    }
}