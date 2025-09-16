<template>
  <div class="chatModel-container">
    <el-card shadow="hover" :body-style="{ paddingBottom: '0' }">
      <el-form :model="queryParams" ref="queryForm" labelWidth="90">
        <el-row>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10">
            <el-form-item label="关键字">
              <el-input v-model="queryParams.searchKey" clearable="" placeholder="请输入模糊查询关键字"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="模型">
              <el-input v-model="queryParams.modelId" clearable="" placeholder="请输入模型"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="模型">
              <el-input v-model="queryParams.name" clearable="" placeholder="请输入模型"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="模型类型">
              <el-select clearable="" v-model="queryParams.modelType" placeholder="请选择模型类型">
                <el-option v-for="(item,index) in dl('code_gen_net_type')" :key="index" :value="item.code" :label="`[${item.code}] ${item.value}`" />
                
              </el-select>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="模型种类供应商">
              <el-input v-model="queryParams.category" clearable="" placeholder="请输入模型种类供应商"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="模型描述">
              <el-input v-model="queryParams.desc" clearable="" placeholder="请输入模型描述"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="6" :xl="6" class="mb10">
            <el-form-item>
              <el-button-group>
                <el-button type="primary"  icon="ele-Search" @click="handleQuery" v-auth="'chatModel:page'"> 查询 </el-button>
                <el-button icon="ele-Refresh" @click="() => queryParams = {}"> 重置 </el-button>
                <el-button icon="ele-ZoomIn" @click="changeAdvanceQueryUI" v-if="!showAdvanceQueryUI"> 高级 </el-button>
                <el-button icon="ele-ZoomOut" @click="changeAdvanceQueryUI" v-if="showAdvanceQueryUI"> 隐藏 </el-button>
                
              </el-button-group>
              
              <el-button-group style="margin-left:20px">
                <el-button type="primary" icon="ele-Plus" @click="openAddChatModel" v-auth="'chatModel:add'"> 新增 </el-button>
                
              </el-button-group>
              
            </el-form-item>
            
          </el-col>
        </el-row>
      </el-form>
    </el-card>
    <el-card class="full-table" shadow="hover" style="margin-top: 8px">
      <el-table
				:data="tableData"
				style="width: 100%"
				v-loading="loading"
				tooltip-effect="light"
				row-key="id"
                @sort-change="sortChange"
				border="">
        <el-table-column type="index" label="序号" width="55" align="center"/>
        <el-table-column prop="modelId" label="模型" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="name" label="模型" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="shortName" label="模型短称" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="avatarUrl" label="模型头像" width="140"  show-overflow-tooltip="" />
          <el-table-column prop="modelType" label="模型类型" width="140"  show-overflow-tooltip="" >
            <template #default="scope">
              <el-tag :type="di('code_gen_net_type', scope.row.modelType)?.tagType"> {{di("code_gen_net_type", scope.row.modelType)?.value}} </el-tag>
            </template>
          </el-table-column>
        <el-table-column prop="category" label="模型种类供应商" width="105"  show-overflow-tooltip="" />
        <el-table-column prop="url" label="接口地址" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="maxToken" label="最大token数量" width="135"  show-overflow-tooltip="" />
        <el-table-column prop="desc" label="模型描述" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="thousandTokenCoin" label="千个token多少钱" width="150"  show-overflow-tooltip="" />
        <el-table-column prop="tags" label="模型标签逗号分割" width="120"  show-overflow-tooltip="" />
        <el-table-column label="操作" width="140" align="center" fixed="right" show-overflow-tooltip="" v-if="auth('chatModel:edit') || auth('chatModel:delete')">
          <template #default="scope">
            <el-button icon="ele-Edit" size="small" text="" type="primary" @click="openEditChatModel(scope.row)" v-auth="'chatModel:edit'"> 编辑 </el-button>
            <el-button icon="ele-Delete" size="small" text="" type="primary" @click="delChatModel(scope.row)" v-auth="'chatModel:delete'"> 删除 </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
				v-model:currentPage="tableParams.page"
				v-model:page-size="tableParams.pageSize"
				:total="tableParams.total"
				:page-sizes="[10, 20, 50, 100, 200, 500]"
				small=""
				background=""
				@size-change="handleSizeChange"
				@current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper"
	/>
      <printDialog
        ref="printDialogRef"
        :title="printChatModelTitle"
        @reloadTable="handleQuery" />
      <editDialog
        ref="editDialogRef"
        :title="editChatModelTitle"
        @reloadTable="handleQuery"
      />
    </el-card>
  </div>
</template>

<script lang="ts" setup="" name="chatModel">
  import { ref } from "vue";
  import { ElMessageBox, ElMessage } from "element-plus";
  import { auth } from '/@/utils/authFunction';
  import { getDictDataItem as di, getDictDataList as dl } from '/@/utils/dict-utils';
  import { formatDate } from '/@/utils/formatTime';


  import printDialog from '/@/views/system/print/component/hiprint/preview.vue'
  import editDialog from '/@/views/main/chatModel/component/editDialog.vue'
  import { pageChatModel, deleteChatModel } from '/@/api/main/chatModel';


  const showAdvanceQueryUI = ref(false);
  const printDialogRef = ref();
  const editDialogRef = ref();
  const loading = ref(false);
  const tableData = ref<any>([]);
  const queryParams = ref<any>({});
  const tableParams = ref({
    page: 1,
    pageSize: 10,
    total: 0,
  });

  const printChatModelTitle = ref("");
  const editChatModelTitle = ref("");

  // 改变高级查询的控件显示状态
  const changeAdvanceQueryUI = () => {
    showAdvanceQueryUI.value = !showAdvanceQueryUI.value;
  }
  

  // 查询操作
  const handleQuery = async () => {
    loading.value = true;
    var res = await pageChatModel(Object.assign(queryParams.value, tableParams.value));
    tableData.value = res.data.result?.items ?? [];
    tableParams.value.total = res.data.result?.total;
    loading.value = false;
  };

  // 列排序
  const sortChange = async (column: any) => {
	queryParams.value.field = column.prop;
	queryParams.value.order = column.order;
	await handleQuery();
  };

  // 打开新增页面
  const openAddChatModel = () => {
    editChatModelTitle.value = '添加AIModels';
    editDialogRef.value.openDialog({});
  };

  // 打开打印页面
  const openPrintChatModel = async (row: any) => {
    printChatModelTitle.value = '打印AIModels';
  }
  
  // 打开编辑页面
  const openEditChatModel = (row: any) => {
    editChatModelTitle.value = '编辑AIModels';
    editDialogRef.value.openDialog(row);
  };

  // 删除
  const delChatModel = (row: any) => {
    ElMessageBox.confirm(`确定要删除吗?`, "提示", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "warning",
  })
  .then(async () => {
    await deleteChatModel(row);
    handleQuery();
    ElMessage.success("删除成功");
  })
  .catch(() => {});
  };

  // 改变页面容量
  const handleSizeChange = (val: number) => {
    tableParams.value.pageSize = val;
    handleQuery();
  };

  // 改变页码序号
  const handleCurrentChange = (val: number) => {
    tableParams.value.page = val;
    handleQuery();
  };

  handleQuery();
</script>
<style scoped>
:deep(.el-ipnut),
:deep(.el-select),
:deep(.el-input-number) {
	width: 100%;
}
</style>

